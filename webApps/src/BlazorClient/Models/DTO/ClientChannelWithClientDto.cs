namespace BlazorClient.Models.DTO;

public class ClientChannelWithClientDto
{
    public ClientChannelDto Channel { get; set; } = null!;
    public string ClientDisplayName { get; set; } = "";
}
