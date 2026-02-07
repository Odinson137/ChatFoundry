using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Interfaces;
using WorkflowService.Utils;

namespace WorkflowService.Models.Workflow;

public sealed class WorkflowGraph(
    IReadOnlyDictionary<Guid, WorkflowNode> nodes,
    IReadOnlyList<WorkflowEdge> edges)
{
    public IReadOnlyDictionary<Guid, WorkflowNode> Nodes { get; } = nodes;
    public IReadOnlyList<WorkflowEdge> Edges { get; } = edges;

    public WorkflowNode GetNode(Guid id)
        => Nodes.TryGetValue(id, out var node)
            ? node
            : throw new InvalidOperationException($"Node '{id}' not found");

    public WorkflowNode GetStartNode()
        => Nodes.Values.First(n => n.Type == WorkflowNodeType.Start);

    public WorkflowNode? GetNextNode(Guid completedActionNodeId, Session session, IVariableService variableService)
    {
        var outgoingEdges = Edges
            .Where(e => e.From == completedActionNodeId)
            .ToList();

        if (outgoingEdges.Count == 0)
            return null;

        foreach (var outgoingEdge in outgoingEdges)
        {
            if (outgoingEdge.Condition != null &&
                WorkflowConditionEvaluator.Evaluate(outgoingEdge.Condition, session, variableService))
            {
                return GetNode(outgoingEdge.To);
            }
        }

        var nextNodeId = outgoingEdges.FirstOrDefault(c => c.Condition == null)?.To;
        return nextNodeId == null ? null : GetNode(nextNodeId.Value);
    }

}