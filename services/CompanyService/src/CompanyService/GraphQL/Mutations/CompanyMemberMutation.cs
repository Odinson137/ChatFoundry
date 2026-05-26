using Billing.Grpc;
using CompanyService.Data;
using CompanyService.Entities;
using CompanyService.Enums;
using Grpc.Core;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Shared.Grpc.Identity;
using Shared.Infrastructure.GraphQl;

namespace CompanyService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class CompanyMemberMutation(
    IHttpContextAccessor httpContextAccessor,
    UserCompanyService.UserCompanyServiceClient identityGrpc,
    IConfiguration configuration,
    global::Billing.Grpc.BillingQuotaService.BillingQuotaServiceClient billingClient) : BaseGraphQl(httpContextAccessor)
{
    public async Task<CompanyMember> AddMember(
        Guid companyId,
        Guid userId,
        CompanyRole role,
        [Service] CompanyDbContext context,
        CancellationToken ct)
    {
        if (configuration.GetValue("Billing:Enabled", true))
        {
            try
            {
                var count = await context.CompanyMembers.CountAsync(m => m.CompanyId == companyId && m.IsActive, ct);
                var r = await billingClient.CheckQuotaAsync(new CheckQuotaRequest
                {
                    CompanyId = companyId.ToString("D"),
                    QuotaType = "team_members",
                    ReportedUsage = count
                }, cancellationToken: ct);
                if (!r.Allowed)
                    throw new GraphQLException(
                        $"Team size quota exceeded. Limit {r.Limit}, current {r.Used}.");
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Unavailable)
            {
            }
        }

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
