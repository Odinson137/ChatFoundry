using ClientService.Entities;
using ClientService.Interfaces;
using Microsoft.EntityFrameworkCore;
using Shared.Domain.Enums;
using WorkflowService.Data;

namespace ClientService.Repositories;

public class ClientChannelRepository(ClientDbContext db)
    : IClientChannelRepository
{
    public Task<ClientChannel?> FindAsync(
        DefaultChannels channel,
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
}