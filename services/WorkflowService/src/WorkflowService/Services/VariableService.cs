using Workflow.Grpc.Client;
using WorkflowService.Entities;
using WorkflowService.Interfaces;

namespace WorkflowService.Services;

public class VariableService(ClientAttributesService.ClientAttributesServiceClient grpcClient) : IVariableService
{
    private const string ClientPrefix = "client.";

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
                        session.Variables[ClientPrefix + key] = value;
                    }
                }
            }

            foreach (var (key, value) in response.CustomAttributes)
            {
                session.Variables[ClientPrefix + key] = value;
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

        if (key.StartsWith(ClientPrefix))
        {
            session.ClientProfileDirty = true;
        }

        session.Variables[key] = value?.ToString() ?? string.Empty;
    }

    public string? GetVariable(Session session, string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be empty", nameof(key));

        return session.Variables.GetValueOrDefault(key);
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
            if (!key.StartsWith(ClientPrefix)) continue;

            var attributeName = key[ClientPrefix.Length..];

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
