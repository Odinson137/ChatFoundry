using Grpc.Core;
using Microsoft.Extensions.Options;
using Shared.Grpc.Company;

namespace IdentityServer.Services;

public class CompanyRegistrationClient(
    CompanyRegistrationService.CompanyRegistrationServiceClient grpcClient,
    IOptions<CompanyServiceOptions> options) : ICompanyRegistrationClient
{
    public async Task<(Guid CompanyId, bool Success)> CreateCompanyWithOwnerAsync(string name, Guid ownerUserId, CancellationToken ct = default)
    {
        try
        {
            var response = await grpcClient.CreateCompanyWithOwnerAsync(
                new CreateCompanyWithOwnerRequest
                {
                    Name = name,
                    OwnerUserId = ownerUserId.ToString()
                },
                cancellationToken: ct);
            return (Guid.Parse(response.CompanyId), true);
        }
        catch (RpcException)
        {
            return (default, false);
        }
    }

    public async Task<Guid?> ConsumeInviteAsync(string inviteToken, Guid userId, CancellationToken ct = default)
    {
        try
        {
            var response = await grpcClient.ConsumeInviteAsync(
                new ConsumeInviteRequest
                {
                    InviteToken = inviteToken,
                    UserId = userId.ToString()
                },
                cancellationToken: ct);
            return Guid.Parse(response.CompanyId);
        }
        catch (RpcException)
        {
            return null;
        }
    }

    public async Task RollbackMemberAsync(Guid companyId, Guid userId, CancellationToken ct = default)
    {
        try
        {
            await grpcClient.RollbackMemberAsync(
                new RollbackMemberRequest
                {
                    CompanyId = companyId.ToString(),
                    UserId = userId.ToString()
                },
                cancellationToken: ct);
        }
        catch (RpcException)
        {
            // best effort
        }
    }
}

public class CompanyServiceOptions
{
    public const string SectionName = "CompanyService";
    public string GrpcAddress { get; set; } = "http://company-service:8081";
}

