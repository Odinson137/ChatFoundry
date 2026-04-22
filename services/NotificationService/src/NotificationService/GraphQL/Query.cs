using HotChocolate.Data;
using NotificationService.Data;
using NotificationService.Entities;
using Shared.Infrastructure.GraphQl;

namespace NotificationService.GraphQL;

public class Query(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<LiveChatSession> GetLiveChatSessions([Service] NotificationDbContext context)
    {
        var query = context.LiveChatSessions
            .Where(s => s.CompanyId == CompanyId!.Value)
            .OrderByDescending(s => s.CreatedAt)
            .AsQueryable();
        return query;
    }

    [UseProjection]
    public IQueryable<LiveChatSession> GetLiveChatSession(
        Guid id,
        [Service] NotificationDbContext context)
    {
        return context.LiveChatSessions.Where(s => s.Id == id);
    }
}