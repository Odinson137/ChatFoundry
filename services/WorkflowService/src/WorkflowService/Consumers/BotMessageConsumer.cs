using MassTransit;
using Shared.Application.Events;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Utils;

namespace WorkflowService.Consumers;

public class BotMessageConsumer(
    ISessionResolver sessionResolver,
    IActionFactory actionFactory,
    IActionRepository actionRepository,
    ITopicProducer<ExecuteActionCommand> producer,
    WorkflowGraphParser workflowGraphParser
    ) : IConsumer<BotIncomingMessage>
{
    public async Task Consume(ConsumeContext<BotIncomingMessage> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;
        
        Console.WriteLine($"Bot message: {msg.Payload}");
        
        var session = await sessionResolver.ResolveAsync(msg, ct);

        var graph = workflowGraphParser.Parse(session.Workflow.NodesDefinition, session.Workflow.EdgesDefinition);

        var currentNode = session.CurrentNodeId == null
            ? graph.GetStartNode() 
            : graph.GetNode(session.CurrentNodeId.Value);

        var action = await actionFactory.CreateAsync(
            session,
            currentNode,
            currentNode.Type == WorkflowNodeType.Ask ? WorkflowNodeType.Input : currentNode.Type,
            msg.Payload,
            ct);
        await actionRepository.AddAsync(action, ct);

        await producer.Produce(new ExecuteActionCommand(action.Id, msg.ExternalUserId, msg.Channel), ct);
        
        Console.WriteLine($"Bot message finished: {msg.Payload}");
    }
}


