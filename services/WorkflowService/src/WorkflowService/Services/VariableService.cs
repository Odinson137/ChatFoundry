using Shared.Domain.Enums;
using Workflow.Grpc.Client;
using WorkflowService.Entities;
using WorkflowService.Interfaces;
using WorkflowService.Enums;
using WorkflowService.Models.Node;
using WorkflowService.Utils;

namespace WorkflowService.Services;

public class VariableService(
    IClientAttributesGrpcClient grpcClient,
    WorkflowGraphParser workflowGraphParser,
    IConfiguration configuration,
    ILogger<VariableService> logger) : IVariableService
{
    private const string GlobalPrefix = "$global.";

    private static readonly Dictionary<string, Func<BaseAttributes, string?>> BaseAttrGetters = new()
    {
        ["name"] = a => a.Name,
        ["username"] = a => a.Username,
        ["phone"] = a => a.Phone,
        ["email"] = a => a.Email
    };

    private static readonly Dictionary<MessageParameter, string> EventParamToGlobalKey = new()
    {
        [MessageParameter.FirstName] = "name",
        [MessageParameter.UserName] = "username",
        [MessageParameter.Mail] = "email",
        [MessageParameter.Phone] = "phone"
    };

    public void PopulateFromEventParameters(Session session, IReadOnlyDictionary<MessageParameter, string> parameters)
    {
        if (parameters == null) return;
        foreach (var (param, key) in EventParamToGlobalKey)
        {
            if (parameters.TryGetValue(param, out var value) && !string.IsNullOrWhiteSpace(value))
                session.Variables[GlobalPrefix + key] = value;
        }
    }

    public async Task LoadClientVariablesAsync(Session session, CancellationToken ct)
    {
        const int maxAttempts = 3;
        var delayMs = 200;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var response = await grpcClient.GetClientAttributesAsync(
                    new GetClientAttributesRequest
                    {
                        ExternalUserId = session.ClientId,
                        Channel = session.Channel.ToString()
                    }, cancellationToken: ct);

                if (response.BaseAttributes != null)
                {
                    foreach (var (key, getter) in BaseAttrGetters)
                    {
                        var value = getter(response.BaseAttributes);
                        if (value != null)
                            session.Variables[GlobalPrefix + key] = value;
                    }
                }

                foreach (var (key, value) in response.CustomAttributes)
                    session.Variables[GlobalPrefix + key] = value;

                return;
            }
            catch (global::Grpc.Core.RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.NotFound)
            {
                if (attempt == maxAttempts - 1)
                    return;
                await Task.Delay(delayMs, ct);
                delayMs *= 2;
            }
        }
    }

    public void SetVariable(Session session, string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be empty", nameof(key));

        if (key.StartsWith(GlobalPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Запись в глобальные атрибуты ($global.*) разрешена только через блок «Атрибут». Используйте узел «Атрибут» для сохранения имени, почты, телефона и других атрибутов.");

        session.Variables[key] = value?.ToString() ?? string.Empty;
    }

    public void SetAttribute(Session session, string attributeName, object? value)
    {
        if (string.IsNullOrWhiteSpace(attributeName))
            throw new ArgumentException("Attribute name cannot be empty", nameof(attributeName));

        var name = attributeName.Trim();
        if (name.StartsWith(GlobalPrefix, StringComparison.OrdinalIgnoreCase))
            name = name[GlobalPrefix.Length..];
        var key = GlobalPrefix + name;
        session.Variables[key] = value?.ToString() ?? string.Empty;
        session.ClientProfileDirty = true;
    }

    private static readonly System.Text.RegularExpressions.Regex NodeOutputKeyRegex = new(
        @"^[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}\.(output|statusCode|error|messageKind)$",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    private string GetBaseUrl()
    {
        var configuredBaseUrl = configuration["Gateway:Url"];
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return configuredBaseUrl.TrimEnd('/');
        }

        return "http://localhost:8080";
    }

    private string? TryResolveCallbackUrl(Session session, string key)
    {
        if (session.Variables.TryGetValue(key, out var cachedUrl) && !string.IsNullOrWhiteSpace(cachedUrl))
        {
            return cachedUrl;
        }

        var parts = key.Split('.');
        if (parts.Length == 3 && Guid.TryParse(parts[1], out var nodeId))
        {
            var graph = workflowGraphParser.Parse(session.Workflow.NodesDefinition, session.Workflow.EdgesDefinition);
            var node = graph.Nodes.GetValueOrDefault(nodeId);
            if (node is { Type: WorkflowNodeType.WebhookWait })
            {
                var webhookWaitData = node.Data as WebhookWaitNodeData;
                var template = webhookWaitData?.CallbackUrlTemplate;
                if (string.IsNullOrWhiteSpace(template))
                {
                    template = "{baseUrl}/workflow/api/webhook/{botId}/{clientId}?channel={channel}";
                }

                var baseUrl = GetBaseUrl();
                var botId = session.Workflow.BotId.ToString();
                var clientId = session.ClientId;
                var channel = session.Channel.ToString().ToLowerInvariant();

                var resolvedUrl = template
                    .Replace("{baseUrl}", baseUrl, StringComparison.OrdinalIgnoreCase)
                    .Replace("{botId}", botId, StringComparison.OrdinalIgnoreCase)
                    .Replace("{clientId}", clientId, StringComparison.OrdinalIgnoreCase)
                    .Replace("{channel}", channel, StringComparison.OrdinalIgnoreCase);

                session.Variables[key] = resolvedUrl;

                return resolvedUrl;
            }
        }
        return null;
    }

    public string? GetVariable(Session session, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be empty", nameof(key));

        var lookupKey = key;
        if (!lookupKey.StartsWith("$node.", StringComparison.OrdinalIgnoreCase) && lookupKey.EndsWith(".callbackUrl", StringComparison.OrdinalIgnoreCase))
        {
            lookupKey = "$node." + lookupKey;
        }

        if (lookupKey.StartsWith("$node.", StringComparison.OrdinalIgnoreCase) && lookupKey.EndsWith(".callbackUrl", StringComparison.OrdinalIgnoreCase))
        {
            var resolvedCallbackUrl = TryResolveCallbackUrl(session, lookupKey);
            if (resolvedCallbackUrl != null)
                return resolvedCallbackUrl;
        }

        if (NodeOutputKeyRegex.IsMatch(key))
        {
            var nodeKey = "$node." + key;
            var value = session.Variables.GetValueOrDefault(nodeKey);
            if (value != null)
                return value;
        }

        // TODO разобраться потом с операндом: надо ли его ставить и хранить в бд или нет
        var value2 = session.Variables.GetValueOrDefault(key);
        if (value2 == null)
        {
            value2 = session.Variables.GetValueOrDefault($"${key}");
        }

        return value2;
    }

    public async Task SyncIfDirtyAsync(Session session, CancellationToken ct)
    {
        if (!session.ClientProfileDirty)
            return;

        var request = new SetClientAttributesRequest
        {
            ExternalUserId = session.ClientId,
            Channel = session.Channel.ToString(),
            BaseAttributes = new BaseAttributes()
        };

        var hasBaseChanges = false;

        foreach (var (key, value) in session.Variables)
        {
            if (!key.StartsWith(GlobalPrefix)) continue;

            var attributeName = key[GlobalPrefix.Length..];

            if (BaseAttrGetters.ContainsKey(attributeName))
            {
                hasBaseChanges = true;
                switch (attributeName)
                {
                    case "name":
                        request.BaseAttributes.Name = value;
                        break;
                    case "username":
                        request.BaseAttributes.Username = value;
                        break;
                    case "phone":
                        request.BaseAttributes.Phone = value;
                        break;
                    case "email":
                        request.BaseAttributes.Email = value;
                        break;
                }
            }
            else
            {
                request.CustomAttributes.Add(attributeName, value);
            }
        }

        if (!hasBaseChanges)
        {
            request.BaseAttributes = null;
        }

        if (!hasBaseChanges && !request.CustomAttributes.Any())
            return;

        const int maxAttempts = 3;
        var delayMs = 200;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                await grpcClient.SetClientAttributesAsync(request, cancellationToken: ct);
                session.ClientProfileDirty = false;
                return;
            }
            catch (global::Grpc.Core.RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.NotFound)
            {
                if (attempt == maxAttempts - 1)
                {
                    logger.LogWarning(
                        "SetClientAttributes failed after {Attempts} retries (NotFound). ClientId={ClientId}, Channel={Channel}. Attributes will be retried on next sync.",
                        maxAttempts, session.ClientId, session.Channel);
                    return;
                }
                await Task.Delay(delayMs, ct);
                delayMs *= 2;
            }
        }
    }
}
