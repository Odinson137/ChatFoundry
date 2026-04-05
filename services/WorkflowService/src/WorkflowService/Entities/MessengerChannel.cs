using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace WorkflowService.Entities;

public class MessengerChannel : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DefaultChannel ChannelType { get; set; }
    public Guid CreatedUserId { get; set; }
    public Guid? CompanyId { get; set; }

    public ICollection<BotChannel> BotChannels { get; set; } = [];

    public string MaskedToken
    {
        get
        {
            if (string.IsNullOrEmpty(Token)) return "—";
            if (Token.Length <= 10) return new string('*', Token.Length);
            return Token.Length > 8 ? Token[..8] + "…" : Token + "…";
        }
    }
}
