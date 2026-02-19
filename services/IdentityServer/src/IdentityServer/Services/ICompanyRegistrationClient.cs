namespace IdentityServer.Services;

public interface ICompanyRegistrationClient
{
    Task<(Guid CompanyId, bool Success)> CreateCompanyWithOwnerAsync(string name, Guid ownerUserId, CancellationToken ct = default);
    Task<Guid?> ConsumeInviteAsync(string inviteToken, Guid userId, CancellationToken ct = default);
    Task RollbackMemberAsync(Guid companyId, Guid userId, CancellationToken ct = default);
}
