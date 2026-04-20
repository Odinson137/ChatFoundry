namespace WorkflowService.Models.Exceptions;

public class SubWorkflowDepthExceededException : InvalidOperationException
{
    public SubWorkflowDepthExceededException(int maxDepth)
        : base($"SubWorkflow nesting depth exceeded (max {maxDepth}). Possible infinite recursion.")
    {
        MaxDepth = maxDepth;
    }

    public int MaxDepth { get; }
}
