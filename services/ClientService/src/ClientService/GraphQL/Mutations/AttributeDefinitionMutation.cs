using ClientService.Data.Enums;
using ClientService.Entities;
using ClientService.Interfaces;
using HotChocolate;
using HotChocolate.Types;
using Shared.Infrastructure.GraphQl;

namespace ClientService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class AttributeDefinitionMutation(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    public async Task<AttributeDefinition> CreateAttributeDefinition(
        AttributeScope scope,
        Guid scopeEntityId,
        string key,
        AttributeType type,
        string? displayName,
        string? description,
        [Service] IAttributeDefinitionRepository attributeDefinitionRepository,
        CancellationToken ct)
    {
        var attributeDefinition = new AttributeDefinition
        {
            Scope = scope,
            ScopeEntityId = scopeEntityId,
            Key = key,
            Type = type,
            DisplayName = displayName,
            Description = description
        };

        await attributeDefinitionRepository.AddAsync(attributeDefinition, ct);

        return attributeDefinition;
    }
}
