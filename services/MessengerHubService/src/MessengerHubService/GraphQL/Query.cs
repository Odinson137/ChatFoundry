using HotChocolate.Data;
using MessengerHubService.Data;
using MessengerHubService.Entities;
using Shared.Infrastructure.GraphQl;

namespace MessengerHubService.GraphQL;

public class Query(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    [UsePaging(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public IQueryable<LiveChatSession> GetLiveChatSessions([Service] MessengerHubDbContext context)
    {
        var query = context.LiveChatSessions.AsQueryable();
        if (CompanyId.HasValue)
            query = query.Where(s => s.CompanyId == CompanyId.Value);
        return query.OrderByDescending(s => s.CreatedAt);
    }

    [UseProjection]
    public IQueryable<LiveChatSession> GetLiveChatSession(
        Guid id,
        [Service] MessengerHubDbContext context)
    {
        return context.LiveChatSessions.Where(s => s.Id == id);
    }
}
