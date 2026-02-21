using Workflow.Grpc.Client;
using WorkflowService.Entities;
using WorkflowService.Interfaces;

namespace WorkflowService.Services;

public class VariableService(ClientAttributesService.ClientAttributesServiceClient grpcClient) : IVariableService
{
    private const string GlobalPrefix = "$global.";

    private static readonly Dictionary<string, Func<BaseAttributes, string?>> BaseAttrGetters = new()
    {
        ["name"] = a => a.Name,
        ["username"] = a => a.Username,
        ["phone"] = a => a.Phone,
        ["email"] = a => a.Email
    };

    public async Task LoadClientVariablesAsync(Session session, CancellationToken ct)
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
                    {
                        session.Variables[GlobalPrefix + key] = value;
                    }
                }
            }

            foreach (var (key, value) in response.CustomAttributes)
            {
                session.Variables[GlobalPrefix + key] = value;
            }
        }
        catch (global::Grpc.Core.RpcException ex) when (ex.StatusCode == global::Grpc.Core.StatusCode.NotFound)
        {
            // Client channel not found yet — no variables to load
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

    public string? GetVariable(Session session, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be empty", nameof(key));

        // TODO разобраться потом с операндом: надо ли его ставить и хранить в бд или нет
        var value = session.Variables.GetValueOrDefault(key);
        if (value == null)
        {
            value = session.Variables.GetValueOrDefault($"${key}");
        }

        return value;
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

        if (hasBaseChanges || request.CustomAttributes.Any())
        {
            await grpcClient.SetClientAttributesAsync(request, cancellationToken: ct);
            session.ClientProfileDirty = false;
        }
    }
}
