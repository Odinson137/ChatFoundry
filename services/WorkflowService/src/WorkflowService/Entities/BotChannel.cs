namespace WorkflowService.Entities;

public class BotChannel
{
    public Guid BotId { get; set; }
    public Bot Bot { get; set; } = null!;

    public Guid ChannelId { get; set; }
    public MessengerChannel Channel { get; set; } = null!;
}
