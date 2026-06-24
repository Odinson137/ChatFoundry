namespace WorkflowService.Models.Node;

public abstract class NodeData
{
    public static readonly NodeData Empty = new EmptyNodeData();
}

public sealed class EmptyNodeData : NodeData { }

