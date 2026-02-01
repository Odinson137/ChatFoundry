using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.GraphQl;
using WorkflowService.Data;
using WorkflowService.Entities;

namespace WorkflowService.GraphQL;

public class Query(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    [UsePaging(IncludeTotalCount = true)]
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public IQueryable<Bot> GetBots([Service] WorkflowDbContext context) 
    {
        var userId = UserId;
        return context.Bots;
    }
    
    [UsePaging(IncludeTotalCount = true)]
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public IQueryable<Session> GetSessions([Service] WorkflowDbContext context)
    {
        return context.Sessions;
    }
    
    [UsePaging(IncludeTotalCount = true)]
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public DbSet<BotWorkflow> GetWorkflows([Service] WorkflowDbContext context)
    {
        return context.Workflows;
    }
        
    [UsePaging(IncludeTotalCount = true)]
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public DbSet<WorkflowVersion> GetWorkflowVersions([Service] WorkflowDbContext context)
    {
        return context.WorkflowVersions;
    }
    
    [UsePaging(IncludeTotalCount = true)]
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public DbSet<ActionEntity> GetActionEntities([Service] WorkflowDbContext context)
    {
        return context.Actions;
    }
}