using ClientService.Data;
using ClientService.Entities;
using ClientService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientService.Repositories;

public class ClientRepository(ClientDbContext db) : IClientRepository
{
    public async Task AddAsync(Client client, CancellationToken ct = default)
    {
        await db.Clients.AddAsync(client, ct);
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        await db.SaveChangesAsync(ct);
    }

    public Task<int> CountByCompanyAsync(Guid? companyId, CancellationToken ct = default)
    {
        return db.Clients.CountAsync(c => c.CompanyId == companyId, ct);
    }
}