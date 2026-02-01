using ClientService.Data;
using ClientService.Entities;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.GraphQl;

namespace ClientService.GraphQL;

public class Query(IHttpContextAccessor httpContextAccessor, ClientDbContext context) : BaseGraphQl(httpContextAccessor)
{
    [UsePaging(IncludeTotalCount = true)]
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public IQueryable<Client> GetClients()
    {
        return context.Clients;
    }
    
    [UsePaging(IncludeTotalCount = true)]
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public IQueryable<ClientChannel> GetClientChannels()
    {
        return context.ClientChannels;
    }
    
    [UsePaging(IncludeTotalCount = true)]
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public DbSet<Message> GetMessages()
    {
        return context.Messages;
    }
}