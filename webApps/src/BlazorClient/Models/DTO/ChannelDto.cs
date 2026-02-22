namespace BlazorClient.Models.DTO;

/// <summary>
/// Канал мессенджера (Telegram и др.). Токен с сервера не отдаётся, только maskedToken.
/// </summary>
public class ChannelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    /// <summary>Тип канала: Telegram, Web, WhatsApp и т.д.</summary>
    public string ChannelType { get; set; } = "Telegram";
    /// <summary>Маскированный токен (вычисляется на сервере).</summary>
    public string MaskedToken { get; set; } = "—";
}
