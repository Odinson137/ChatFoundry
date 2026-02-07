using ClientService.Data.Enums;
using Shared.Domain.Entities;

namespace ClientService.Entities;

public class AttributeDefinition : EntityBase
{
    public string Key { get; set; } = null!;

    public string? DisplayName { get; set; }

    public string? Description { get; set; }

    public AttributeType Type { get; set; } = AttributeType.String;

    public AttributeScope Scope { get; set; }

    public Guid ScopeEntityId { get; set; }
}
