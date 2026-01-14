namespace BlazorClient.Models.DTO;

public class BotDto
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Token { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public List<WorkflowDto> Workflows { get; set; }
}