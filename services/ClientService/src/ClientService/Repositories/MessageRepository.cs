using ClientService.Entities;
using ClientService.Interfaces;
using WorkflowService.Data;

namespace ClientService.Repositories;

public class MessageRepository(ClientDbContext db) : IMessageRepository
{
    public async Task AddAsync(Message message, CancellationToken ct = default)
    {
        await db.Messages.AddAsync(message, ct);
    }
}