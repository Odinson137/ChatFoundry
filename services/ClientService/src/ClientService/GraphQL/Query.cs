using ClientService.Data;
using ClientService.Entities;
using ClientService.GraphQL.Dtos;
using ClientService.Interfaces;
using HotChocolate;
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
    
    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public DbSet<AttributeDefinition> GetAttributes()
    {
        return context.AttributeDefinitions;
    }

    // public async Task<List<AttributeDefinitionDto>> GetAttributeDefinitions(
    //     Guid teamId,
    //     [Service] IAttributeDefinitionRepository repository,
    //     CancellationToken ct)
    // {
    //     var attributes = await repository.GetByTeamIdAsync(teamId, ct);
    //     
    //     return attributes.Select(a => new AttributeDefinitionDto
    //     {
    //         Key = a.Key,
    //         DisplayName = a.DisplayName,
    //         Description = a.Description,
    //         Type = a.Type.ToString()
    //     }).ToList();
    // }
}