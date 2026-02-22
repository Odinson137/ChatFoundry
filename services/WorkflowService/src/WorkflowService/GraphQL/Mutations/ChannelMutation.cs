using HotChocolate;
using HotChocolate.Types;
using MassTransit;
using Shared.Application.Events;
using Shared.Domain.Enums;
using Shared.Infrastructure.GraphQl;
using WorkflowService.Data;
using WorkflowService.Entities;
using WorkflowService.Interfaces;

namespace WorkflowService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class ChannelMutation(
    IHttpContextAccessor httpContextAccessor,
    ITopicProducer<TelegramSetWebhookEvent> producer) : BaseGraphQl(httpContextAccessor)
{
    public async Task<AddChannelPayload> AddChannelAsync(
        AddChannelInput input,
        [Service] WorkflowDbContext context)
    {
        var channel = new MessengerChannel
        {
            Name = input.Name,
            Token = input.Token,
            ChannelType = input.ChannelType,
            CreatedUserId = UserId,
            CompanyId = CompanyId
        };

        context.MessengerChannels.Add(channel);
        await context.SaveChangesAsync();

        return new AddChannelPayload(channel);
    }

    public async Task<UpdateChannelPayload> UpdateChannelAsync(
        UpdateChannelInput input,
        [Service] WorkflowDbContext context)
    {
        var channel = await context.MessengerChannels.FindAsync(input.ChannelId);
        if (channel is null)
            return new UpdateChannelPayload(null);

        channel.Name = input.Name;
        if (!string.IsNullOrEmpty(input.Token))
            channel.Token = input.Token;
        channel.ChannelType = input.ChannelType;
        await context.SaveChangesAsync();

        return new UpdateChannelPayload(channel);
    }

    public async Task<DeleteChannelPayload> DeleteChannelAsync(
        DeleteChannelInput input,
        [Service] WorkflowDbContext context,
        [Service] IChannelRepository channelRepository)
    {
        var channel = await context.MessengerChannels.FindAsync(input.ChannelId);
        if (channel is null)
            return new DeleteChannelPayload(null, null);

        var hasBots = await channelRepository.HasLinkedBotsAsync(input.ChannelId);
        if (hasBots)
            return new DeleteChannelPayload(null, "Канал привязан к одному или нескольким ботам. Сначала отвяжите канал от ботов.");

        context.MessengerChannels.Remove(channel);
        await context.SaveChangesAsync();

        return new DeleteChannelPayload(channel, null);
    }

    /// <summary>Актуализирует webhook для одного канала (отправляет TelegramSetWebhookEvent).</summary>
    public async Task<RefreshChannelWebhookPayload> RefreshChannelWebhookAsync(
        RefreshChannelWebhookInput input,
        [Service] WorkflowDbContext context)
    {
        var channel = await context.MessengerChannels.FindAsync(input.ChannelId);
        if (channel is null)
            return new RefreshChannelWebhookPayload(null);

        await producer.Produce(new TelegramSetWebhookEvent(channel.Id, channel.Token));
        return new RefreshChannelWebhookPayload(channel);
    }
}

public record AddChannelInput(string Name, string Token, DefaultChannel ChannelType);
public record AddChannelPayload(MessengerChannel Channel);

public record UpdateChannelInput(Guid ChannelId, string Name, string? Token, DefaultChannel ChannelType);
public record UpdateChannelPayload(MessengerChannel? Channel);

public record DeleteChannelInput(Guid ChannelId);
public record DeleteChannelPayload(MessengerChannel? Channel, string? Error);

public record RefreshChannelWebhookInput(Guid ChannelId);
public record RefreshChannelWebhookPayload(MessengerChannel? Channel);
