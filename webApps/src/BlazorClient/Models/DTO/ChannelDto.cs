namespace BlazorClient.Models.DTO;

public class ChannelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ChannelType { get; set; } = "Telegram";
    public string MaskedToken { get; set; } = "—";
}
