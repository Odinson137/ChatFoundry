using CompanyService.Data;
using CompanyService.Entities;
using CompanyService.Enums;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Shared.Grpc.Identity;
using Shared.Infrastructure.GraphQl;

namespace CompanyService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class CompanyMemberMutation(
    IHttpContextAccessor httpContextAccessor,
    UserCompanyService.UserCompanyServiceClient identityGrpc) : BaseGraphQl(httpContextAccessor)
{
    public async Task<CompanyMember> AddMember(
        Guid companyId,
        Guid userId,
        CompanyRole role,
        [Service] CompanyDbContext context,
        CancellationToken ct)
    {
        var member = new CompanyMember
        {
            CompanyId = companyId,
            UserId = userId,
            Role = role,
            IsActive = true
        };

        context.CompanyMembers.Add(member);
        await context.SaveChangesAsync(ct);

        return member;
    }

    public async Task<CompanyMember> UpdateMemberRole(
        Guid companyId,
        Guid userId,
        CompanyRole role,
        [Service] CompanyDbContext context,
        CancellationToken ct)
    {
        var member = await context.CompanyMembers
            .FirstOrDefaultAsync(m => m.CompanyId == companyId && m.UserId == userId, ct)
            ?? throw new GraphQLException("Member not found.");

        member.Role = role;
        member.ModifiedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return member;
    }

    public async Task<bool> RemoveMember(
        Guid companyId,
        Guid userId,
        [Service] CompanyDbContext context,
        CancellationToken ct)
    {
        if (!CompanyId.HasValue || CompanyId.Value != companyId)
            throw new GraphQLException("You can only remove members from your company.");

        var caller = await context.CompanyMembers
            .FirstOrDefaultAsync(m => m.CompanyId == companyId && m.UserId == UserId && m.IsActive, ct);
        if (caller == null || (caller.Role != CompanyRole.Owner && caller.Role != CompanyRole.Admin))
            throw new GraphQLException("Only Owner or Admin can remove members.");

        var member = await context.CompanyMembers
            .FirstOrDefaultAsync(m => m.CompanyId == companyId && m.UserId == userId, ct)
            ?? throw new GraphQLException("Member not found.");

        if (member.Role == CompanyRole.Owner)
        {
            var ownerCount = await context.CompanyMembers
                .CountAsync(m => m.CompanyId == companyId && m.Role == CompanyRole.Owner && m.IsActive, ct);
            if (ownerCount <= 1)
                throw new GraphQLException("Cannot remove the last Owner.");
        }

        member.IsActive = false;
        member.ModifiedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        await identityGrpc.ClearUserCompanyAsync(new ClearUserCompanyRequest { UserId = userId.ToString() }, cancellationToken: ct);
        return true;
    }
}
