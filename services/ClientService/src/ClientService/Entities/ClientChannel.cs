using System.Text.Json;
using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace ClientService.Entities;

public class ClientChannel : EntityBase
{
    public DefaultChannel Channel { get; set; }
    
    public string ExternalUserId { get; set; } = null!;
    
    public string? Phone { get; set; }
    
    public string? Email { get; set; }
    
    public string? Username { get; set; }
    
    public string? Name { get; set; }
    
    public Guid ClientId { get; set; }
    
    public Client Client { get; set; } = null!;
    
    public ICollection<Message> Messages { get; set; }
    public IDictionary<string, JsonElement> Attributes { get; set; }
}