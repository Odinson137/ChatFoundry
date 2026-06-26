using HotChocolate;
using HotChocolate.Types;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Events;
using Shared.Infrastructure.GraphQl;
using WorkflowService.Data;
using WorkflowService.Entities;
using WorkflowService.Services;

namespace WorkflowService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class BotMutation(
    IHttpContextAccessor httpContextAccessor,
    IGraphQlCacheService cacheService) : BaseGraphQl(httpContextAccessor)
{
    public async Task<AddBotPayload> AddBotAsync(
        AddBotInput input,
        [Service] WorkflowDbContext context,
        [Service] ITopicProducer<TelegramSetWebhookEvent> producer,
        [Service] BillingQuotaGuard billing,
        CancellationToken ct)
    {
        if (CompanyId.HasValue)
        {
            var count = await context.Bots.CountAsync(b => b.CompanyId == CompanyId, ct);
            try
            {
                await billing.EnsureQuotaAsync(CompanyId, "bots", count, ct);
            }
            catch (InvalidOperationException ex)
            {
                throw new GraphQLException(ex.Message);
            }
        }

        var bot = new Bot
        {
            Name = input.Name,
            CreatedUserId = UserId,
            CompanyId = CompanyId
        };

        context.Bots.Add(bot);
        await context.SaveChangesAsync();

        if (input.ChannelIds is { Length: > 0 })
        {
            foreach (var channelId in input.ChannelIds)
            {
                context.BotChannels.Add(new BotChannel { BotId = bot.Id, ChannelId = channelId });
            }
            await context.SaveChangesAsync();

            try
            {
                await SetWebhooksForBotChannelsAsync(context, bot.Id, input.ChannelIds, producer);
            }
            catch
            {
            }
        }

        await cacheService.EvictByTagsAsync(new[] { $"company:{CompanyId.Value}:bots" }, ct);

        return new AddBotPayload(bot);
    }

    public async Task<UpdateBotPayload> UpdateBotAsync(
        UpdateBotInput input,
        [Service] WorkflowDbContext context,
        [Service] ITopicProducer<TelegramSetWebhookEvent> producer)
    {
        var bot = await context.Bots
            .Include(b => b.BotChannels)
            .FirstOrDefaultAsync(b => b.Id == input.BotId);

        if (bot is null)
            return new UpdateBotPayload(null);

        bot.Name = input.Name;

        var currentChannelIds = bot.BotChannels.Select(bc => bc.ChannelId).ToHashSet();
        var newChannelIds = (input.ChannelIds ?? Array.Empty<Guid>()).ToHashSet();

        var toRemove = currentChannelIds.Except(newChannelIds).ToList();
        var toAdd = newChannelIds.Except(currentChannelIds).ToList();

        foreach (var channelId in toRemove)
        {
            var bc = bot.BotChannels.First(bc => bc.ChannelId == channelId);
            context.BotChannels.Remove(bc);
        }
        foreach (var channelId in toAdd)
        {
            context.BotChannels.Add(new BotChannel { BotId = bot.Id, ChannelId = channelId });
        }

        await context.SaveChangesAsync();

        if (toAdd.Count > 0)
        {
            try
            {
                await SetWebhooksForBotChannelsAsync(context, bot.Id, toAdd.ToArray(), producer);
            }
            catch
            {
            }
        }

        await cacheService.EvictByTagsAsync(new[] { $"company:{CompanyId.Value}:bots", $"bot:{bot.Id}" });

        return new UpdateBotPayload(bot);
    }

    public async Task<RefreshWebhookPayload> RefreshBotWebhookAsync(
        RefreshBotWebhookInput input,
        [Service] WorkflowDbContext context,
        [Service] ITopicProducer<TelegramSetWebhookEvent> producer)
    {
        var channelIds = await context.BotChannels
            .Where(bc => bc.BotId == input.BotId)
            .Select(bc => bc.ChannelId)
            .ToArrayAsync();

        if (channelIds.Length > 0)
            await SetWebhooksForBotChannelsAsync(context, input.BotId, channelIds, producer);

        var bot = await context.Bots.FindAsync(input.BotId);
        return new RefreshWebhookPayload(bot);
    }

    public async Task<DeleteBotPayload> DeleteBotAsync(
        DeleteBotInput input,
        [Service] WorkflowDbContext context)
    {
        var bot = await context.Bots
            .Include(b => b.BotChannels)
            .FirstOrDefaultAsync(b => b.Id == input.BotId);

        if (bot is null)
            return new DeleteBotPayload(null);

        context.BotChannels.RemoveRange(bot.BotChannels);
        context.Bots.Remove(bot);
        await context.SaveChangesAsync();

        await cacheService.EvictByTagsAsync(new[] { $"company:{CompanyId.Value}:bots", $"bot:{bot.Id}" });

        return new DeleteBotPayload(bot);
    }

    private static async Task SetWebhooksForBotChannelsAsync(
        WorkflowDbContext context,
        Guid botId,
        Guid[] channelIds,
        ITopicProducer<TelegramSetWebhookEvent> producer)
    {
        var channels = await context.MessengerChannels
            .Where(c => channelIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Token })
            .ToListAsync();

        foreach (var ch in channels)
        {
            await producer.Produce(new TelegramSetWebhookEvent(ch.Id, ch.Token));
        }
    }
}

public record AddBotInput(string Name, Guid[]? ChannelIds);
public record AddBotPayload(Bot Bot);

public record UpdateBotInput(Guid BotId, string Name, Guid[]? ChannelIds);
public record UpdateBotPayload(Bot? Bot);

public record DeleteBotInput(Guid BotId);
public record DeleteBotPayload(Bot? Bot);

public record RefreshBotWebhookInput(Guid BotId);
public record RefreshWebhookPayload(Bot? Bot);
