using ClientService.Data;
using ClientService.Entities;
using ClientService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ClientService.Repositories;

public class AttributeDefinitionRepository : IAttributeDefinitionRepository
{
    private readonly ClientDbContext _context;

    public AttributeDefinitionRepository(ClientDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(AttributeDefinition attributeDefinition, CancellationToken ct)
    {
        await _context.AttributeDefinitions.AddAsync(attributeDefinition, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<AttributeDefinition?> FindByKeyAsync(Guid teamId, string key, CancellationToken ct)
    {
        return await _context.AttributeDefinitions
            .FirstOrDefaultAsync(ad => ad.TeamId == teamId && ad.Key == key, ct);
    }

    public async Task<List<AttributeDefinition>> GetByTeamIdAsync(Guid teamId, CancellationToken ct)
    {
        return await _context.AttributeDefinitions
            .Where(a => a.TeamId == teamId)
            .OrderBy(a => a.Key)
            .ToListAsync(ct);
    }
}
