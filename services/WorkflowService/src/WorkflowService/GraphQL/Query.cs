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
        if (!CompanyId.HasValue)
            throw new GraphQLException("Company ID is required.");

        return context.Bots.Where(c => c.CompanyId == CompanyId.Value);
    }

    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<MessengerChannel> GetChannels([Service] WorkflowDbContext context)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("Company ID is required.");

        return context.MessengerChannels.Where(c => c.CompanyId == CompanyId.Value);
    }

    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Session> GetSessions([Service] WorkflowDbContext context)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("Company ID is required.");

        return context.Sessions
            .Where(s => s.Workflow.Bot.CompanyId == CompanyId.Value)
            .OrderByDescending(s => s.CreatedAt);
    }

    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<BotWorkflow> GetWorkflows([Service] WorkflowDbContext context)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("Company ID is required.");

        return context.Workflows.Where(w => w.Bot.CompanyId == CompanyId.Value);
    }

    [UsePaging(IncludeTotalCount = true)]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<ActionEntity> GetActionEntities([Service] WorkflowDbContext context)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("Company ID is required.");

        return context.Actions.Where(a => a.Session.Workflow.Bot.CompanyId == CompanyId.Value);
    }
}