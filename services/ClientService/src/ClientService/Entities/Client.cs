using Shared.Domain.Entities;

namespace ClientService.Entities;

public class Client : EntityBase
{
    public Guid? CompanyId { get; set; }
    public string? DisplayName { get; set; }

    public ICollection<ClientChannel> ClientChannels { get; set; } = [];
    public ICollection<AttributeDefinition> Attributes { get; set; } = [];
}