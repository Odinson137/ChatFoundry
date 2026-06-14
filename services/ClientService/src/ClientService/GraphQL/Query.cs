using ClientService.Data;
using ClientService.Entities;
using ClientService.Interfaces;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.GraphQl;

namespace ClientService.GraphQL;

public class Query(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Client> GetClients(
        [Service] ClientDbContext context,
        string? search = null)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("Company ID is required.");

        var query = context.Clients
            .Where(c => c.CompanyId != null && c.CompanyId == CompanyId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                (c.DisplayName != null && c.DisplayName.ToLower().Contains(term)) ||
                c.ClientChannels.Any(ch =>
                    (ch.Name != null && ch.Name.ToLower().Contains(term)) ||
                    (ch.LastName != null && ch.LastName.ToLower().Contains(term)) ||
                    (ch.Username != null && ch.Username.ToLower().Contains(term)) ||
                    (ch.Phone != null && ch.Phone.Contains(term)) ||
                    (ch.Email != null && ch.Email.ToLower().Contains(term))));
        }

        return query;
    }

    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ClientChannel> GetClientChannels([Service] ClientDbContext context)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("Company ID is required.");

        return context.ClientChannels.Where(ch => ch.Client.CompanyId != null && ch.Client.CompanyId == CompanyId.Value);
    }

    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Message> GetMessages([Service] ClientDbContext context)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("Company ID is required.");

        return context.Messages.Where(m => m.ClientChannel != null && m.ClientChannel.Client.CompanyId == CompanyId.Value);
    }

    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<AttributeDefinition> GetAttributes([Service] ClientDbContext context)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("Company ID is required.");

        return context.AttributeDefinitions.Where(a => a.ScopeEntityId == CompanyId.Value);
    }

    /// <summary>
    /// Атрибуты компании текущего пользователя (Scope = Company, ScopeEntityId = CompanyId из JWT).
    /// Для вкладки «Атрибуты» на странице компании и для списка в редакторе workflow.
    /// </summary>
    public async Task<List<AttributeDefinition>> GetCompanyAttributeDefinitions(
        [Service] IAttributeDefinitionRepository repository,
        CancellationToken ct)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("Company ID is required.");

        return await repository.GetByScopeEntityIdAsync(CompanyId.Value, ct);
    }
}
