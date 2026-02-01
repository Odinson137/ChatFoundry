using HotChocolate;
using HotChocolate.Types;
using MassTransit;
using Shared.Application.Events;
using Shared.Infrastructure.GraphQl;
using WorkflowService.Data;
using WorkflowService.Entities;

namespace WorkflowService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class BotMutation(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    public async Task<AddBotPayload> AddBotAsync(
        AddBotInput input,
        [Service] WorkflowDbContext context, 
        [Service] ITopicProducer<TelegramSetWebhookEvent> producer)
    {
        var bot = new Bot
        {
            Name = input.Name,
            Token = input.Token,
            CreatedUserId = UserId
        };

        context.Bots.Add(bot);
        await context.SaveChangesAsync();

        await producer.Produce(new TelegramSetWebhookEvent(bot.Id, bot.Token));
        
        return new AddBotPayload(bot);
    }

    public async Task<UpdateBotPayload> UpdateBotAsync(
        UpdateBotInput input,
        [Service] WorkflowDbContext context, 
        [Service] ITopicProducer<TelegramSetWebhookEvent> producer)
    {
        var bot = await context.Bots.FindAsync(input.BotId);

        if (bot is null)
        {
            return new UpdateBotPayload(null); 
        }

        bot.Name = input.Name;

        var isTokenChanged = bot.Token != input.Token;

        bot.Token = input.Token;

        await context.SaveChangesAsync();

        if (isTokenChanged)
        {
            await producer.Produce(new TelegramSetWebhookEvent(bot.Id, bot.Token));
        }
        
        return new UpdateBotPayload(bot);
    }
    
    public async Task<RefreshWebhookPayload> RefreshBotWebhookAsync(
        RefreshBotWebhookInput input,
        [Service] WorkflowDbContext context,
        [Service] ITopicProducer<TelegramSetWebhookEvent> producer)
    {
        var bot = await context.Bots.FindAsync(input.BotId);
        if (bot is null)
            return new RefreshWebhookPayload(null);

        await producer.Produce(new TelegramSetWebhookEvent(bot.Id, bot.Token));

        return new RefreshWebhookPayload(bot);
    }

    public async Task<DeleteBotPayload> DeleteBotAsync(
        DeleteBotInput input,
        [Service] WorkflowDbContext context)
    {
        var bot = await context.Bots.FindAsync(input.BotId);

        if (bot is null)
        {
            return new DeleteBotPayload(null);
        }
        context.Bots.Remove(bot);
        await context.SaveChangesAsync();

        return new DeleteBotPayload(bot);
    }
}

public record AddBotInput(string Name, string Token);
public record AddBotPayload(Bot Bot);

public record UpdateBotInput(Guid BotId, string Name, string Token);
public record UpdateBotPayload(Bot? Bot);

public record DeleteBotInput(Guid BotId);
public record DeleteBotPayload(Bot? Bot);

public record RefreshBotWebhookInput(Guid BotId);
public record RefreshWebhookPayload(Bot? Bot);
