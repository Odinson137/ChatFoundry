using ClientService.Entities;

namespace ClientService.Interfaces;

public interface IAttributeDefinitionRepository
{
    Task AddAsync(AttributeDefinition attributeDefinition, CancellationToken ct);
    Task<AttributeDefinition?> FindByKeyAsync(Guid scopeEntityId, string key, CancellationToken ct);
    Task<List<AttributeDefinition>> GetByScopeEntityIdAsync(Guid scopeEntityId, CancellationToken ct);
}
