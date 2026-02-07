using Shared.Domain.Entities;

namespace ClientService.Entities;

public class Client : EntityBase
{
    public string? DisplayName { get; set; }
    
    public ICollection<ClientChannel> ClientChannels { get; set; } = [];
    
    public ICollection<AttributeDefinition> Attributes { get; set; } = [];
}