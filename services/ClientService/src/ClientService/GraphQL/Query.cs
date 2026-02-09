using ClientService.Data;
using ClientService.Entities;
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
    public IQueryable<Client> GetClients([Service] ClientDbContext context)
    {
        return context.Clients;
    }

    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ClientChannel> GetClientChannels([Service] ClientDbContext context)
    {
        return context.ClientChannels;
    }

    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public DbSet<Message> GetMessages([Service] ClientDbContext context)
    {
        return context.Messages;
    }

    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public DbSet<AttributeDefinition> GetAttributes([Service] ClientDbContext context)
    {
        return context.AttributeDefinitions;
    }
}
