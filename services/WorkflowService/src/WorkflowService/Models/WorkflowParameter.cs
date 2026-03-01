namespace WorkflowService.Models;

public class WorkflowParameter
{
    public string Name { get; set; } = "";
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
}
