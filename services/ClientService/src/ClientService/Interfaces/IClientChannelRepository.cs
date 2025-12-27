using ClientService.Entities;
using Shared.Domain.Enums;

namespace ClientService.Interfaces;

public interface IClientChannelRepository
{
    Task<ClientChannel?> FindAsync(
        DefaultChannels channel,
        string externalUserId,
        CancellationToken ct = default);

    Task AddAsync(ClientChannel clientChannel, CancellationToken ct = default);
}