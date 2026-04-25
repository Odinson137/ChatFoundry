using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Shared.Application.Events;
using WorkflowService.Entities;
using WorkflowService.Enums;
using WorkflowService.Events;
using WorkflowService.Interfaces;
using WorkflowService.Utils;

namespace WorkflowService.Consumers;

public class BotMessageConsumer(
    IBotRepository botRepository,
    ISessionResolver sessionResolver,
    IActionFactory actionFactory,
    IActionRepository actionRepository,
    ITopicProducer<ExecuteActionCommand> producer,
    WorkflowGraphParser workflowGraphParser,
    IDistributedCache distributedCache) : IConsumer<BotIncomingMessage>
{
    public async Task Consume(ConsumeContext<BotIncomingMessage> context)
    {
        var msg = context.Message;
        var ct = context.CancellationToken;

        var redisKey = $"livechat:{msg.ChannelId}:{msg.ExternalUserId}";
        var isActiveLiveChat = await distributedCache.GetStringAsync(redisKey, ct);
        if (isActiveLiveChat != null)
            return;

        var botIds = await botRepository.GetBotIdsByChannelIdAsync(msg.ChannelId, ct);
        if (botIds.Count == 0)
            return;

        foreach (var botId in botIds)
        {
            Session? session = null;
            try
            {
                session = await sessionResolver.ResolveForBotAsync(msg, botId, ct);

                var graph = workflowGraphParser.Parse(session.Workflow.NodesDefinition, session.Workflow.EdgesDefinition);
                var currentNode = graph.GetNode(session.CurrentNodeId!.Value);

                var action = await actionFactory.CreateAsync(
                    session,
                    currentNode,
                    currentNode.Type == WorkflowNodeType.Ask ? WorkflowNodeType.Input : currentNode.Type,
                    msg.Payload,
                    msg.MessageKind,
                    ct);
                await actionRepository.AddAsync(action, ct);

                await producer.Produce(new ExecuteActionCommand(action.Id, msg.ExternalUserId, msg.Channel), ct);
            }
            catch
            {
                if (session != null)
                    await sessionResolver.CloseSessionAndHierarchyAsync(session.Id, ct);
            }
        }
    }
}

