using Shared.Domain.Enums;
using Workflow.Grpc.Client;
using WorkflowService.Entities;
using WorkflowService.Interfaces;

namespace WorkflowService.Services;

public class VariableService(
    IClientAttributesGrpcClient grpcClient,
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
                    return; // Client still not created; PopulateFromEventParameters already set base attrs
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

    /// <summary>
    /// Pattern: guid.output or guid.statusCode — stored in workflow without "node." prefix; resolved as $node.&lt;guid&gt;.&lt;key&gt;.
    /// </summary>
    private static readonly System.Text.RegularExpressions.Regex NodeOutputKeyRegex = new(
        @"^[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}\.(output|statusCode)$",
        System.Text.RegularExpressions.RegexOptions.Compiled);

    public string? GetVariable(Session session, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be empty", nameof(key));

        // Short form from DB: {{guid.output}} / {{guid.statusCode}} → resolve as $node.guid.output
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
                    return; // Do not throw — workflow continues; ClientProfileDirty stays true for next sync
                }
                await Task.Delay(delayMs, ct);
                delayMs *= 2;
            }
        }
    }
}
