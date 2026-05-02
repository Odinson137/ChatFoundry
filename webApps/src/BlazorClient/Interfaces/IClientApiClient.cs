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
    Task<Guid?> GetClientIdByChannelIdAsync(Guid clientChannelId);

    Task<MessagesPageResult> GetMessagesAsync(Guid clientChannelId, int first, string? after = null);

    Task<MessagesPageResult> GetMessagesByChannelAsync(Guid channelId, string externalUserId, string channel, int first);

    Task<List<AttributeDefinitionDto>> GetCompanyAttributeDefinitionsAsync(CancellationToken ct = default);
    Task<AttributeDefinitionDto> CreateCompanyAttributeDefinitionAsync(string key, string? displayName, string? description, CancellationToken ct = default);
    Task<AttributeDefinitionDto?> UpdateAttributeDefinitionAsync(Guid id, string? displayName, string? description, AttributeType? type, CancellationToken ct = default);
    Task<bool> DeleteAttributeDefinitionAsync(Guid id, CancellationToken ct = default);

    Task<ClientChannelDto?> SetClientChannelAttributesAsync(SetClientChannelAttributesRequest request, CancellationToken ct = default);
}

public enum AttributeScope { Company, Bot }
public enum AttributeType { String, Number, Boolean, Date, Json }
