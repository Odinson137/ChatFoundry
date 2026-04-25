using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Interfaces;
using WorkflowService.Utils;
using Shared.Domain.Enums;

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

    public WorkflowNode GetStartNode(BotIncomingMessageSource source = BotIncomingMessageSource.Client)
    {
        var startNodeType = source switch
        {
            BotIncomingMessageSource.Timer => WorkflowNodeType.TimerStart,
            _ => WorkflowNodeType.Start,
        };

        // TODO добавить обработчик ошибок
        return Nodes.Values.First(n => n.Type == startNodeType);
    }

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
                var node = GetNode(outgoingEdge.To);
                return node;
            }
        }

        var nextNodeId = outgoingEdges.FirstOrDefault(c => c.Condition == null)?.To;
        if (nextNodeId == null) return null;
        var next = GetNode(nextNodeId.Value);
        return next;
    }

}
