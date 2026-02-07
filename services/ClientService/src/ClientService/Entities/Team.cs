using Shared.Domain.Entities;

namespace ClientService.Entities;

// TODO переименовать в компани и возможно создать отдельный сервис
public class Team : EntityBase
{
    public string Name { get; set; } = null!;
    
    public ICollection<AttributeDefinition> AttributeDefinitions { get; set; } = [];
    
    public ICollection<Client> Clients { get; set; } = [];
}
