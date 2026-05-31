using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.GraphQl;
using Shared.Domain.Enums;
using WorkflowService.Data;
using WorkflowService.Entities;

namespace WorkflowService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class SessionMutation(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    public async Task<CompleteSessionPayload> CompleteSessionAsync(
        CompleteSessionInput input,
        [Service] WorkflowDbContext context,
        CancellationToken ct)
    {
        var session = await context.Sessions
            .Include(s => s.Workflow)
            .ThenInclude(w => w.Bot)
            .FirstOrDefaultAsync(s => s.Id == input.SessionId, ct);

        if (session == null)
            return new CompleteSessionPayload(null, "Сессия не найдена.");

        if (CompanyId.HasValue && session.Workflow?.Bot?.CompanyId != CompanyId.Value)
            return new CompleteSessionPayload(null, "У вас нет доступа к этой сессии.");

        if (session.Status != SessionStatus.Completed && session.Status != SessionStatus.Failed && session.Status != SessionStatus.Cancelled)
        {
            session.Status = SessionStatus.Completed;
            session.CompletedAt = DateTime.UtcNow;
            await context.SaveChangesAsync(ct);
        }

        return new CompleteSessionPayload(session);
    }
}

public record CompleteSessionInput(Guid SessionId);
public record CompleteSessionPayload(Session? Session, string? Error = null);
