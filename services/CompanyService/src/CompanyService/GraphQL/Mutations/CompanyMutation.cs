using CompanyService.Data;
using CompanyService.Entities;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.GraphQl;

namespace CompanyService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class CompanyMutation(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    public async Task<Company> CreateCompany(
        string name,
        string? description,
        int maxUsers,
        [Service] CompanyDbContext context,
        CancellationToken ct)
    {
        var company = new Company
        {
            Name = name,
            Description = description,
            MaxUsers = maxUsers
        };

        context.Companies.Add(company);
        await context.SaveChangesAsync(ct);

        return company;
    }

    public async Task<Company> UpdateCompany(
        Guid id,
        string? name,
        string? description,
        int? maxUsers,
        [Service] CompanyDbContext context,
        CancellationToken ct)
    {
        var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new GraphQLException("Company not found.");

        if (name is not null) company.Name = name;
        if (description is not null) company.Description = description;
        if (maxUsers.HasValue) company.MaxUsers = maxUsers.Value;

        company.ModifiedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(ct);

        return company;
    }

    public async Task<bool> DeleteCompany(
        Guid id,
        [Service] CompanyDbContext context,
        CancellationToken ct)
    {
        var company = await context.Companies.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new GraphQLException("Company not found.");

        context.Companies.Remove(company);
        await context.SaveChangesAsync(ct);

        return true;
    }
}
