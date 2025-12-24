namespace WorkflowService.Models.Workflow;

public class WorkflowEdge
{
    public Guid From { get; init; }
    public Guid To { get; init; }
    public WorkflowCondition? Condition { get; init; }
}