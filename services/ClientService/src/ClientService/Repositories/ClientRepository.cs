using ClientService.Data;
using ClientService.Entities;
using ClientService.Interfaces;

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
}