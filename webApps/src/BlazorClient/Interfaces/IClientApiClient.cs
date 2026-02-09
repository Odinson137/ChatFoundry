using BlazorClient.Models.DTO;

namespace BlazorClient.Interfaces;

public interface IClientApiClient
{
    Task<ClientsPageResult> GetClientsAsync(
        int first,
        string? after = null,
        string? search = null,
        string? channelFilter = null,
        string sortField = "createdAt",
        string sortDirection = "DESC");

    Task<ClientDto?> GetClientByIdAsync(Guid clientId);

    Task<MessagesPageResult> GetMessagesAsync(Guid clientChannelId, int first, string? after = null);
}
