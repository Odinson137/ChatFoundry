using CompanyService.Data;
using CompanyService.Entities;
using CompanyService.Enums;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.GraphQl;

namespace CompanyService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class CompanyMemberMutation(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
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
        var member = await context.CompanyMembers
            .FirstOrDefaultAsync(m => m.CompanyId == companyId && m.UserId == userId, ct)
            ?? throw new GraphQLException("Member not found.");

        context.CompanyMembers.Remove(member);
        await context.SaveChangesAsync(ct);

        return true;
    }
}
