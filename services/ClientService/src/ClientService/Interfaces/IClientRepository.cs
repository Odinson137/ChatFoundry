using ClientService.Entities;

namespace ClientService.Interfaces;

public interface IClientRepository
{
    Task AddAsync(Client client, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
    Task<int> CountByCompanyAsync(Guid? companyId, CancellationToken ct = default);
}