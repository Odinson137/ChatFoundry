using Workflow.Grpc.Client;

namespace WorkflowService.Interfaces;

public interface IClientAttributesGrpcClient
{
    Task<GetClientAttributesResponse> GetClientAttributesAsync(GetClientAttributesRequest request, CancellationToken cancellationToken = default);
    Task<SetClientAttributesResponse> SetClientAttributesAsync(SetClientAttributesRequest request, CancellationToken cancellationToken = default);
}
