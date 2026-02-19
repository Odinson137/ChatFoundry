using CompanyService.Data;
using CompanyService.Entities;
using HotChocolate.Data;
using HotChocolate.Types;
using Shared.Infrastructure.GraphQl;

namespace CompanyService.GraphQL;

public class Query(IHttpContextAccessor httpContextAccessor, CompanyDbContext context) : BaseGraphQl(httpContextAccessor)
{
    [UsePaging(IncludeTotalCount = true, MaxPageSize = 100)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Company> GetCompanies()
    {
        if (!CompanyId.HasValue) return context.Companies.Where(_ => false);
        return context.Companies.Where(c => c.Id == CompanyId.Value);
    }

    [UsePaging(IncludeTotalCount = true, MaxPageSize = 100)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<CompanyMember> GetCompanyMembers()
    {
        if (!CompanyId.HasValue) return context.CompanyMembers.Where(_ => false);
        return context.CompanyMembers.Where(m => m.CompanyId == CompanyId.Value);
    }

    [UsePaging(IncludeTotalCount = true, MaxPageSize = 100)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Invitation> GetInvitations()
    {
        if (!CompanyId.HasValue) return context.Invitations.Where(_ => false);
        return context.Invitations.Where(i => i.CompanyId == CompanyId.Value);
    }
}
