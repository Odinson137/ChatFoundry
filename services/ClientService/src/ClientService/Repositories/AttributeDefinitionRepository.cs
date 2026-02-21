using ClientService.Data;
using ClientService.Entities;
using ClientService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientService.Repositories;

public class AttributeDefinitionRepository(ClientDbContext context) : IAttributeDefinitionRepository
{
    public async Task AddAsync(AttributeDefinition attributeDefinition, CancellationToken ct)
    {
        await context.AttributeDefinitions.AddAsync(attributeDefinition, ct);
        await context.SaveChangesAsync(ct);
    }

    public async Task<AttributeDefinition?> FindByIdAsync(Guid id, CancellationToken ct)
    {
        return await context.AttributeDefinitions.FirstOrDefaultAsync(ad => ad.Id == id, ct);
    }

    public async Task<AttributeDefinition?> FindByKeyAsync(Guid scopeEntityId, string key, CancellationToken ct)
    {
        return await context.AttributeDefinitions
            .FirstOrDefaultAsync(ad => ad.ScopeEntityId == scopeEntityId && ad.Key == key, ct);
    }

    public async Task<List<AttributeDefinition>> GetByScopeEntityIdAsync(Guid scopeEntityId, CancellationToken ct)
    {
        return await context.AttributeDefinitions
            .Where(a => a.ScopeEntityId == scopeEntityId)
            .OrderBy(a => a.Key)
            .ToListAsync(ct);
    }

    public async Task UpdateAsync(AttributeDefinition attributeDefinition, CancellationToken ct)
    {
        context.AttributeDefinitions.Update(attributeDefinition);
        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(AttributeDefinition attributeDefinition, CancellationToken ct)
    {
        context.AttributeDefinitions.Remove(attributeDefinition);
        await context.SaveChangesAsync(ct);
    }
}
