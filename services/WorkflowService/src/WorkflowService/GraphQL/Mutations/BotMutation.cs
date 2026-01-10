using HotChocolate;
using WorkflowService.Data;
using WorkflowService.Entities;

namespace WorkflowService.GraphQL.Mutations;

public class BotMutation
{
    public async Task<AddBotPayload> AddBotAsync(
        AddBotInput input,
        [Service] WorkflowDbContext context)
    {
        var bot = new Bot
        {
            Name = input.Name,
            Token = input.Token 
        };

        context.Bots.Add(bot);
        await context.SaveChangesAsync();

        return new AddBotPayload(bot);
    }

    public async Task<UpdateBotPayload> UpdateBotAsync(
        UpdateBotInput input,
        [Service] WorkflowDbContext context)
    {
        var bot = await context.Bots.FindAsync(input.BotId);

        if (bot is null)
        {
            return new UpdateBotPayload(null); 
        }

        bot.Name = input.Name;

        await context.SaveChangesAsync();

        return new UpdateBotPayload(bot);
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

public record UpdateBotInput(Guid BotId, string Name);
public record UpdateBotPayload(Bot? Bot);

public record DeleteBotInput(Guid BotId);
public record DeleteBotPayload(Bot? Bot);