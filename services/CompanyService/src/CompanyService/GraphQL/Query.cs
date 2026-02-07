using CompanyService.Data;
using CompanyService.Entities;
using HotChocolate.Data;
using HotChocolate.Types;
using Shared.Infrastructure.GraphQl;

namespace CompanyService.GraphQL;

public class Query(IHttpContextAccessor httpContextAccessor, CompanyDbContext context) : BaseGraphQl(httpContextAccessor)
{
    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Company> GetCompanies()
    {
        return context.Companies;
    }

    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<CompanyMember> GetCompanyMembers()
    {
        return context.CompanyMembers;
    }
}
