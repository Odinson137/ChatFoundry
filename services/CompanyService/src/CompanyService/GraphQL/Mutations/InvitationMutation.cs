using CompanyService.Data;
using CompanyService.Entities;
using CompanyService.Enums;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.GraphQl;

namespace CompanyService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class InvitationMutation(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    public async Task<InvitationResult> CreateInvitation(
        string? email,
        CompanyRole role,
        int expiresInDays,
        string baseUrl,
        [Service] CompanyDbContext context,
        CancellationToken ct)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("Company is not set in the token.");

        var companyId = CompanyId.Value;

        var member = await context.CompanyMembers
            .FirstOrDefaultAsync(m => m.CompanyId == companyId && m.UserId == UserId && m.IsActive, ct);
        if (member == null)
            throw new GraphQLException("You are not a member of this company.");
        if (member.Role != CompanyRole.Owner && member.Role != CompanyRole.Admin)
            throw new GraphQLException("Only Owner or Admin can create invitations.");

        if (expiresInDays < 1 || expiresInDays > 365)
            throw new GraphQLException("expiresInDays must be between 1 and 365.");

        var invitation = new Invitation
        {
            CompanyId = companyId,
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Role = role,
            ExpiresAt = DateTime.UtcNow.AddDays(expiresInDays)
        };
        context.Invitations.Add(invitation);
        await context.SaveChangesAsync(ct);

        var link = $"{baseUrl.TrimEnd('/')}/register?invite={invitation.Id}";
        return new InvitationResult(invitation.Id, link, invitation.ExpiresAt);
    }
}

public record InvitationResult(Guid Id, string InviteLink, DateTime ExpiresAt);
