using Google.Protobuf.WellKnownTypes;
using Workflow.Grpc.Client;
using WorkflowService.Entities;
using WorkflowService.Interfaces;

namespace WorkflowService.Services;

public class UserProfileSyncer : IUserProfileSyncer
{
    private readonly ClientAttributesService.ClientAttributesServiceClient _client;
    private static readonly HashSet<string> BaseKeys = new() { "name", "username", "phone", "email" };

    public UserProfileSyncer(ClientAttributesService.ClientAttributesServiceClient client)
    {
        _client = client;
    }

    public async Task SyncAsync(Session session, CancellationToken ct)
    {
        if (!session.UserProfileDirty)
        {
            return;
        }

        var request = new SetClientAttributesRequest
        {
            ExternalUserId = session.ClientId,
            Channel = session.Channel.ToString(),
            BaseAttributes = new BaseAttributes()
        };

        var hasBaseChanges = false;

        foreach (var (key, value) in session.Variables)
        {
            if (!key.StartsWith("user.")) continue;

            var attributeName = key.Substring(5); // Remove "user." prefix

            if (BaseKeys.Contains(attributeName))
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
            await _client.SetClientAttributesAsync(request, cancellationToken: ct);
        }
    }
}
