using ClientService.Data;
using ClientService.Entities;
using ClientService.Interfaces;

namespace ClientService.Repositories;

public class MessageRepository(ClientDbContext db) : IMessageRepository
{
    public async Task AddAsync(Message message, CancellationToken ct = default)
    {
        await db.Messages.AddAsync(message, ct);
    }
}