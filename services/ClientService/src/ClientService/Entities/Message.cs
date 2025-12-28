using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace ClientService.Entities;

public class Message : EntityBase
{
    public string? Payload { get; set; }
    
    public MessageDirection Direction { get; set; }
    
    public MessageKind MessageKind { get; set; } = MessageKind.Text;
    
    public string? InternalMessageId { get; set; }
    public Guid? CreatedById { get; set; }
    
    public ClientChannel? ClientChannel { get; set; }
}