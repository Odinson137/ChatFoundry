using ClientService.Data;
using HotChocolate;

namespace ClientService.GraphQL.Mutations;

public class ClientChannelMutation
{
    public async Task<OkPayload> UpdateClientChannelAsync(
        UpdateClientChannelInput input,
        [Service] ClientDbContext context)
    {
        // var bot = await context.Bots.FindAsync(input.BotId);
        //
        // if (bot is null)
        // {
        //     return new UpdateBotPayload(null); 
        // }
        //
        // bot.Name = input.Name;
        //
        // await context.SaveChangesAsync();

        return new OkPayload("Data has successfully updated.");
    }
}

public record UpdateClientChannelInput(Guid ClientChannelId, string DisplayName);


