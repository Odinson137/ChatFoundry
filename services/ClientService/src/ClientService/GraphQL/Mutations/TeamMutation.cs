using ClientService.Data.Enums;
using ClientService.Entities;
using ClientService.Interfaces;
using HotChocolate;
using HotChocolate.Types;
using Shared.Infrastructure.GraphQl;

namespace ClientService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class TeamMutation(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    public async Task<Team> CreateTeam(
        string name,
        [Service] ITeamRepository teamRepository,
        CancellationToken ct)
    {
        var team = new Team
        {
            Name = name
        };

        await teamRepository.AddAsync(team, ct);
        
        return team;
    }

    public async Task<AttributeDefinition> CreateAttributeDefinition(
        Guid teamId,
        string key,
        AttributeType type,
        string? displayName,
        string? description,
        [Service] IAttributeDefinitionRepository attributeDefinitionRepository,
        CancellationToken ct)
    {
        var attributeDefinition = new AttributeDefinition
        {
            TeamId = teamId,
            Key = key,
            Type = type,
            DisplayName = displayName,
            Description = description
        };

        await attributeDefinitionRepository.AddAsync(attributeDefinition, ct);
        
        return attributeDefinition;
    }
}
