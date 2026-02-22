namespace BlazorClient.Models.DTO;

public class BotDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public List<WorkflowDto> Workflows { get; set; } = [];
    /// <summary>Каналы, привязанные к боту (для отображения в таблице).</summary>
    public List<BotChannelDto> BotChannels { get; set; } = [];
}

/// <summary>Связь бота с каналом для отображения названий каналов.</summary>
public class BotChannelDto
{
    public Guid ChannelId { get; set; }
    public ChannelRefDto? Channel { get; set; }
}

public class ChannelRefDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}