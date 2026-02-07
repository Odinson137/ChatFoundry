using ClientService.Entities;
using Shared.Domain.Enums;

namespace ClientService.Interfaces;

public interface IClientChannelRepository
{
    Task<ClientChannel?> FindAsync(
        DefaultChannel channel,
        string externalUserId,
        CancellationToken ct = default);

    Task<ClientChannel?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(ClientChannel clientChannel, CancellationToken ct = default);

    Task SaveAsync(ClientChannel clientChannel, CancellationToken ct = default);
}