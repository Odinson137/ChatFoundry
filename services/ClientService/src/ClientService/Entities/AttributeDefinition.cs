using ClientService.Data.Enums;
using Shared.Domain.Entities;

namespace ClientService.Entities;

public class AttributeDefinition : EntityBase
{
    public Guid TeamId { get; set; }
    
    public string Key { get; set; } = null!;
    
    public string? DisplayName { get; set; }
    
    public string? Description { get; set; }
    
    public AttributeType Type { get; set; } = AttributeType.String;
    
    public Team Team { get; set; } = null!;
}
