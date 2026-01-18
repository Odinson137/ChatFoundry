using ClientService.Data;
using ClientService.Entities;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.GraphQl;

namespace ClientService.GraphQL;

public class Query(IHttpContextAccessor httpContextAccessor, ClientDbContext context) : BaseGraphQl(httpContextAccessor)
{
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public IQueryable<Client> GetClients()
    {
        return context.Clients;
    }
    
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public IQueryable<ClientChannel> GetClientChannels()
    {
        return context.ClientChannels;
    }
    
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public DbSet<Message> GetMessages()
    {
        return context.Messages;
    }
}