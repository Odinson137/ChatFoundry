using MassTransit;
using Shared.Application.Events;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Utils;

namespace WorkflowService.Consumers;

public class ActionCompletedConsumer(
    IActionFactory actionFactory,
    IActionRepository actionRepository,
    ISessionResolver sessionResolver,
    ITopicProducer<ExecuteActionCommand> producer,
    WorkflowGraphParser workflowGraphParser)
    : IConsumer<ActionCompletedEvent>
{
    public async Task Consume(ConsumeContext<ActionCompletedEvent> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        
        var lastUserAction = await actionRepository.GetAsync(msg.Channel, msg.ClientId, ct);
        if (lastUserAction == null)
            return;
        

        var session = lastUserAction.Session;
        
        lastUserAction.MarkCompleted();
        session.CompletedAt = DateTime.UtcNow;
        
        var graph = workflowGraphParser.Parse(session.Workflow.SchemaJson);

        var nextNode = graph.GetNextNode(lastUserAction.NodeId, session);
        if (nextNode == null)
        {
            await sessionResolver.CloseSessionAsync(session.Id, ct);
            return;
        }
        
        if (lastUserAction.WorkflowNodeType == WorkflowNodeType.Ask)
            return;
        
        session.MoveTo(nextNode.Id);

        var nextAction = await actionFactory.CreateAsync(
            session,
            nextNode,
            nextNode.Type,
            cancellationToken: ct);

        await actionRepository.AddAsync(nextAction, ct);

        await producer.Produce(new ExecuteActionCommand(nextAction.Id, msg.ClientId, msg.Channel), ct);
    }
}
