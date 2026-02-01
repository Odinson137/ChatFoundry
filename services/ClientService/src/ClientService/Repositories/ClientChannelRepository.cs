using ClientService.Data;
using ClientService.Entities;
using ClientService.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;

namespace ClientService.Repositories;

public class ClientChannelRepository(ClientDbContext db)
    : IClientChannelRepository
{
    public Task<ClientChannel?> FindAsync(
        DefaultChannel channel,
        string externalUserId,
        CancellationToken ct = default)
    {
        return db.ClientChannels
            .Include(x => x.Client)
            .FirstOrDefaultAsync(x =>
                x.Channel == channel &&
                x.ExternalUserId == externalUserId, ct);
    }

    public async Task AddAsync(ClientChannel clientChannel, CancellationToken ct = default)
    {
        await db.ClientChannels.AddAsync(clientChannel, ct);
    }

    public Task SaveAsync(ClientChannel clientChannel, CancellationToken ct = default)
    {
        return db.SaveChangesAsync(ct);
    }
}