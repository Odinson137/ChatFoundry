using Shared.Domain.Entities;

namespace ClientService.Entities;

public class ClientAttribute : EntityBase
{
    public string Key { get; set; } = null!;

    public string Value { get; set; } = string.Empty;

    public Guid ClientChannelId { get; set; }

    public ClientChannel ClientChannel { get; set; } = null!;
}
