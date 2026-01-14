namespace BlazorClient.Models.DTO;

public class WorkflowDto
{
    public Guid Id { get; set; }
    public string BotId { get; set; }
    public string SchemaJson { get; set; }
    public int Version { get; set; }
    public bool IsActiveBotWorkflow { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
}