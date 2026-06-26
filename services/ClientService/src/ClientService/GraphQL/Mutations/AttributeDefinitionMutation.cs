using ClientService.Data.Enums;
using ClientService.Entities;
using ClientService.Interfaces;
using HotChocolate;
using HotChocolate.Types;
using Shared.Infrastructure.GraphQl;

namespace ClientService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class AttributeDefinitionMutation(
    IHttpContextAccessor httpContextAccessor,
    IGraphQlCacheService cacheService) : BaseGraphQl(httpContextAccessor)
{
    public async Task<AttributeDefinition> CreateCompanyAttributeDefinition(
        string key,
        string? displayName,
        string? description,
        [Service] IAttributeDefinitionRepository attributeDefinitionRepository,
        CancellationToken ct)
    {
        if (!CompanyId.HasValue)
            throw new UnauthorizedAccessException("Нет доступа к компании.");
        var attributeDefinition = new AttributeDefinition
        {
            Scope = AttributeScope.Company,
            ScopeEntityId = CompanyId.Value,
            Key = key,
            Type = AttributeType.String,
            DisplayName = displayName,
            Description = description
        };

        await attributeDefinitionRepository.AddAsync(attributeDefinition, ct);

        await cacheService.EvictByTagsAsync(new[] { $"company:{CompanyId.Value}:attributes" }, ct);

        return attributeDefinition;
    }

    public async Task<AttributeDefinition> CreateAttributeDefinition(
        AttributeScope scope,
        Guid scopeEntityId,
        string key,
        string? displayName,
        string? description,
        [Service] IAttributeDefinitionRepository attributeDefinitionRepository,
        CancellationToken ct)
    {
        if (scope == AttributeScope.Company && (!CompanyId.HasValue || scopeEntityId != CompanyId.Value))
            throw new UnauthorizedAccessException("Можно создавать атрибуты только для своей компании.");
        var attributeDefinition = new AttributeDefinition
        {
            Scope = scope,
            ScopeEntityId = scopeEntityId,
            Key = key,
            Type = AttributeType.String,
            DisplayName = displayName,
            Description = description
        };

        await attributeDefinitionRepository.AddAsync(attributeDefinition, ct);

        if (scope == AttributeScope.Company)
        {
            await cacheService.EvictByTagsAsync(new[] { $"company:{CompanyId.Value}:attributes" }, ct);
        }

        return attributeDefinition;
    }

    public async Task<AttributeDefinition?> UpdateAttributeDefinition(
        Guid id,
        string? displayName,
        string? description,
        AttributeType? type,
        [Service] IAttributeDefinitionRepository attributeDefinitionRepository,
        CancellationToken ct)
    {
        if (!CompanyId.HasValue) return null;
        var existing = await attributeDefinitionRepository.FindByIdAsync(id, ct);
        if (existing == null || existing.Scope != AttributeScope.Company || existing.ScopeEntityId != CompanyId.Value)
            return null;
        if (displayName != null) existing.DisplayName = displayName;
        if (description != null) existing.Description = description;
        if (type.HasValue) existing.Type = type.Value;
        await attributeDefinitionRepository.UpdateAsync(existing, ct);
        await cacheService.EvictByTagsAsync(new[] { $"company:{CompanyId.Value}:attributes" }, ct);
        return existing;
    }

    public async Task<bool> DeleteAttributeDefinition(
        Guid id,
        [Service] IAttributeDefinitionRepository attributeDefinitionRepository,
        CancellationToken ct)
    {
        if (!CompanyId.HasValue) return false;
        var existing = await attributeDefinitionRepository.FindByIdAsync(id, ct);
        if (existing == null || existing.Scope != AttributeScope.Company || existing.ScopeEntityId != CompanyId.Value)
            return false;
        await attributeDefinitionRepository.DeleteAsync(existing, ct);
        await cacheService.EvictByTagsAsync(new[] { $"company:{CompanyId.Value}:attributes" }, ct);
        return true;
    }
}
