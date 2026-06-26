using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Shared.Infrastructure.GraphQl;

public class GraphQlResultCacheMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GraphQlResultCacheMiddleware> _logger;

    public GraphQlResultCacheMiddleware(
        RequestDelegate next,
        ILogger<GraphQlResultCacheMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IGraphQlCacheService cacheService,
        IOptions<GraphQlCacheOptions> cacheOptions,
        IConnectionMultiplexer redis)
    {
        var path = context.Request.Path.Value;
        if (path == null || !path.EndsWith("/graphql", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var method = context.Request.Method;
        string? query = null;
        string? operationName = null;
        string? hash = null;
        JsonElement? variables = null;

        if (HttpMethods.IsGet(method))
        {
            query = context.Request.Query["query"];
            operationName = context.Request.Query["operationName"];
            var variablesStr = context.Request.Query["variables"];
            if (!string.IsNullOrEmpty(variablesStr))
            {
                try { variables = JsonSerializer.Deserialize<JsonElement>(variablesStr); } catch { }
            }

            var extensionsStr = context.Request.Query["extensions"];
            if (!string.IsNullOrEmpty(extensionsStr))
            {
                try
                {
                    using var doc = JsonDocument.Parse(extensionsStr);
                    if (doc.RootElement.TryGetProperty("persistedQuery", out var pq) &&
                        pq.TryGetProperty("sha256Hash", out var h))
                    {
                        hash = h.GetString();
                    }
                }
                catch { }
            }
        }
        else if (HttpMethods.IsPost(method))
        {
            context.Request.EnableBuffering();
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
            {
                var bodyStr = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (!string.IsNullOrWhiteSpace(bodyStr))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(bodyStr);
                        var root = doc.RootElement;
                        if (root.TryGetProperty("query", out var q)) query = q.GetString();
                        if (root.TryGetProperty("operationName", out var op)) operationName = op.GetString();
                        if (root.TryGetProperty("variables", out var vars)) variables = vars.Clone();
                        if (root.TryGetProperty("extensions", out var ext) &&
                            ext.TryGetProperty("persistedQuery", out var pq) &&
                            pq.TryGetProperty("sha256Hash", out var h))
                        {
                            hash = h.GetString();
                        }
                    }
                    catch { }
                }
            }
        }

        var db = redis.GetDatabase();

        if (hash != null && query != null)
        {
            await db.StringSetAsync($"cf:apq:query:{hash}", query, TimeSpan.FromDays(30));
            if (string.IsNullOrEmpty(operationName))
            {
                operationName = ExtractOperationName(query);
            }
            if (!string.IsNullOrEmpty(operationName))
            {
                await db.StringSetAsync($"cf:apq:opname:{hash}", operationName, TimeSpan.FromDays(30));
            }
        }
        else if (hash != null && query == null)
        {
            query = await db.StringGetAsync($"cf:apq:query:{hash}");
            if (query == null)
            {
                context.Response.ContentType = "application/json";
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync(
                    "{\"errors\":[{\"message\":\"PersistedQueryNotFound\",\"extensions\":{\"code\":\"PERSISTED_QUERY_NOT_FOUND\"}}]}",
                    Encoding.UTF8
                );
                return;
            }

            if (HttpMethods.IsGet(method))
            {
                var queryItems = context.Request.Query.ToDictionary(x => x.Key, x => x.Value);
                queryItems["query"] = query;
                context.Request.Query = new QueryCollection(queryItems.ToDictionary(
                    k => k.Key,
                    v => new Microsoft.Extensions.Primitives.StringValues(v.Value.ToArray())
                ));
            }
            else if (HttpMethods.IsPost(method))
            {
                var payload = new
                {
                    query = query,
                    variables = variables,
                    extensions = new
                    {
                        persistedQuery = new
                        {
                            version = 1,
                            sha256Hash = hash
                        }
                    }
                };
                var newBodyBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
                context.Request.Body = new MemoryStream(newBodyBytes);
                context.Request.ContentLength = newBodyBytes.Length;
            }

            if (string.IsNullOrEmpty(operationName))
            {
                operationName = await db.StringGetAsync($"cf:apq:opname:{hash}");
                if (string.IsNullOrEmpty(operationName))
                {
                    operationName = ExtractOperationName(query);
                }
            }
        }
        else if (hash == null && query != null)
        {
            hash = ComputeSha256(query);
            if (string.IsNullOrEmpty(operationName))
            {
                operationName = ExtractOperationName(query);
            }
            if (!string.IsNullOrEmpty(operationName))
            {
                await db.StringSetAsync($"cf:apq:opname:{hash}", operationName, TimeSpan.FromDays(30));
            }
            await db.StringSetAsync($"cf:apq:query:{hash}", query, TimeSpan.FromDays(30));
        }

        var options = cacheOptions.Value;
        var isCacheable = operationName != null && options.CacheableQueries.Contains(operationName);

        var isMutation = false;
        if (query != null && query.TrimStart().StartsWith("mutation", StringComparison.OrdinalIgnoreCase))
        {
            isMutation = true;
        }

        if (!isCacheable || isMutation || hash == null)
        {
            await _next(context);
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        var companyId = context.User.FindFirstValue("company_id");

        var varsHash = variables.HasValue ? ComputeSha256(JsonSerializer.Serialize(variables.Value)) : "none";
        var cacheKey = $"{hash}:{varsHash}:{(companyId ?? "nocompany")}:{(userId ?? "nouser")}";

        var cachedResponse = await cacheService.GetAsync(cacheKey, context.RequestAborted);
        if (cachedResponse != null)
        {
            _logger.LogInformation("GraphQL Cache Hit for operation {OperationName}, key: {CacheKey}", operationName, cacheKey);
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync(cachedResponse, Encoding.UTF8);
            return;
        }

        _logger.LogInformation("GraphQL Cache Miss for operation {OperationName}, executing...", operationName);

        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        try
        {
            await _next(context);

            if (context.Response.StatusCode == 200)
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                var responseText = await new StreamReader(responseBody).ReadToEndAsync();
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);

                if (!responseText.Contains("\"errors\""))
                {
                    var templates = options.QueryTags.TryGetValue(operationName, out var t) ? t : new List<string>();
                    var resolvedTags = ResolveTags(templates, companyId, userId, variables);

                    if (!string.IsNullOrEmpty(companyId))
                    {
                        resolvedTags.Add($"company:{companyId}");
                    }

                    var ttlSeconds = options.QueryTtls.TryGetValue(operationName, out var customTtl) ? customTtl : options.DefaultTtlSeconds;
                    var ttl = TimeSpan.FromSeconds(ttlSeconds);

                    await cacheService.SetAsync(cacheKey, responseText, resolvedTags, ttl, context.RequestAborted);
                    _logger.LogInformation("GraphQL Query cached: {OperationName}, tags: {Tags}, TTL: {TTL}s", operationName, string.Join(", ", resolvedTags), ttlSeconds);
                }
            }
            else
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    private static string? ExtractOperationName(string queryText)
    {
        if (string.IsNullOrWhiteSpace(queryText)) return null;

        var match = Regex.Match(
            queryText,
            @"\b(query|mutation)\s+([a-zA-Z0-9_]+)\b",
            RegexOptions.IgnoreCase);

        if (match.Success && match.Groups.Count > 2)
        {
            return match.Groups[2].Value;
        }

        return null;
    }

    private static string ComputeSha256(string input)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static List<string> ResolveTags(List<string> templates, string? companyId, string? userId, JsonElement? variables)
    {
        var tags = new List<string>();
        foreach (var template in templates)
        {
            var resolved = template;
            if (companyId != null)
                resolved = resolved.Replace("{companyId}", companyId, StringComparison.OrdinalIgnoreCase);
            if (userId != null)
                resolved = resolved.Replace("{userId}", userId, StringComparison.OrdinalIgnoreCase);

            if (variables.HasValue && resolved.Contains("{variables.", StringComparison.OrdinalIgnoreCase))
            {
                var matches = Regex.Matches(resolved, @"\{variables\.([a-zA-Z0-9_]+)\}");
                foreach (Match match in matches)
                {
                    var varName = match.Groups[1].Value;
                    if (variables.Value.TryGetProperty(varName, out var prop))
                    {
                        var valStr = prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
                        resolved = resolved.Replace(match.Value, valStr);
                    }
                }
            }
            tags.Add(resolved);
        }
        return tags;
    }
}
