using ClientService.Entities;

namespace ClientService.Interfaces;

public interface IAttributeDefinitionRepository
{
    Task AddAsync(AttributeDefinition attributeDefinition, CancellationToken ct);
    Task<AttributeDefinition?> FindByKeyAsync(Guid teamId, string key, CancellationToken ct);
    Task<List<AttributeDefinition>> GetByTeamIdAsync(Guid teamId, CancellationToken ct);
}
