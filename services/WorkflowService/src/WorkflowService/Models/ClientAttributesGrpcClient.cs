using Workflow.Grpc.Client;
using WorkflowService.Interfaces;

namespace WorkflowService.Models;

public sealed class ClientAttributesGrpcClient(ClientAttributesService.ClientAttributesServiceClient inner) : IClientAttributesGrpcClient
{
    public Task<GetClientAttributesResponse> GetClientAttributesAsync(GetClientAttributesRequest request, CancellationToken cancellationToken = default)
        => inner.GetClientAttributesAsync(request, cancellationToken: cancellationToken).ResponseAsync;

    public Task<SetClientAttributesResponse> SetClientAttributesAsync(SetClientAttributesRequest request, CancellationToken cancellationToken = default)
        => inner.SetClientAttributesAsync(request, cancellationToken: cancellationToken).ResponseAsync;

}
