namespace WorkflowService.Exceptions;

/// <summary>
/// Thrown when SubWorkflow nesting depth exceeds the allowed maximum (e.g. possible infinite recursion).
/// This is a permanent business rule violation — retrying the message will not resolve it.
/// </summary>
public class SubWorkflowDepthExceededException : InvalidOperationException
{
    public SubWorkflowDepthExceededException(int maxDepth)
        : base($"SubWorkflow nesting depth exceeded (max {maxDepth}). Possible infinite recursion.")
    {
        MaxDepth = maxDepth;
    }

    public int MaxDepth { get; }
}
