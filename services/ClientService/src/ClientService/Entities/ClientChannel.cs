using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace ClientService.Entities;

public class ClientChannel : EntityBase
{
    public Guid ClientId { get; set; }
    
    public Client Client { get; set; } = null!;
    
    public DefaultChannel Channel { get; set; }
    
    public string ExternalUserId { get; set; } = null!;
    
    // Merge keys
    public string? Phone { get; set; } // позже для канала телефона сделать автоматическую подстановку сюда из ExternalUserId
    public string? Email { get; set; } // позже для канала почты сделать автоматическую подстановку сюда из ExternalUserId
    // Merge keys
    
    public string? Username { get; set; }

    public ICollection<Message> Messages { get; set; } = [];
}