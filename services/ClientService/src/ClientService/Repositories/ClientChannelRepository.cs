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
        Guid? companyId = null,
        CancellationToken ct = default)
    {
        var query = db.ClientChannels
            .Include(x => x.Client)
            .Where(x => x.Channel == channel && x.ExternalUserId == externalUserId);
        if (companyId.HasValue)
            query = query.Where(x => x.Client.CompanyId == companyId.Value);
        return query.FirstOrDefaultAsync(ct);
    }

    public Task<ClientChannel?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return db.ClientChannels
            .Include(x => x.Client)
            .FirstOrDefaultAsync(x => x.Id == id, ct);
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