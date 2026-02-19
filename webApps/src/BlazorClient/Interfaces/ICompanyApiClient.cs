namespace BlazorClient.Interfaces;

public interface ICompanyApiClient
{
    Task<CompanyDto?> GetCurrentCompanyAsync(CancellationToken ct = default);
    Task<List<CompanyMemberDto>> GetMembersAsync(CancellationToken ct = default);
    Task<List<InvitationDto>> GetInvitationsAsync(CancellationToken ct = default);
    Task<InvitationResultDto> CreateInvitationAsync(string? email, string role, int expiresInDays, string baseUrl, CancellationToken ct = default);
}

public record CompanyDto(Guid Id, string Name, string? Description, int MaxUsers);

public record CompanyMemberDto(Guid Id, Guid UserId, string Role, bool IsActive);

public record InvitationDto(Guid Id, string? Email, string Role, DateTime ExpiresAt, DateTime? UsedAt);

public record InvitationResultDto(Guid Id, string InviteLink, DateTime ExpiresAt);
