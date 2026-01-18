using HotChocolate;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.GraphQl;
using WorkflowService.Data;
using WorkflowService.Entities;

namespace WorkflowService.GraphQL;

public class Query(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public IQueryable<Bot> GetBots([Service] WorkflowDbContext context) 
    {
        var userId = UserId;
        return context.Bots;
    }
    
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public IQueryable<Session> GetSessions([Service] WorkflowDbContext context)
    {
        return context.Sessions;
    }
    
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public DbSet<BotWorkflow> GetWorkflows([Service] WorkflowDbContext context)
    {
        return context.Workflows;
    }
        
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public DbSet<WorkflowVersion> GetWorkflowVersions([Service] WorkflowDbContext context)
    {
        return context.WorkflowVersions;
    }
    
    [UseProjection] 
    [UseFiltering]
    [UseSorting]
    public DbSet<ActionEntity> GetActionEntities([Service] WorkflowDbContext context)
    {
        return context.Actions;
    }
}