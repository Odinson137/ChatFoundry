using ClientService.Data;
using ClientService.Entities;
using HotChocolate;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Shared.Infrastructure.GraphQl;

namespace ClientService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class ClientMutation(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    public async Task<OkPayload> UpdateClientAsync(
        UpdateClientInput input,
        [Service] ClientDbContext context)
    {



        return new OkPayload("Data has successfully updated.");
    }

    public async Task<ClientChannel?> SetClientChannelAttributesAsync(
        SetClientChannelAttributesInput input,
        [Service] ClientDbContext context,
        CancellationToken ct)
    {
        if (!CompanyId.HasValue)
            throw new UnauthorizedAccessException("Нет доступа.");

        var channel = await context.ClientChannels
            .Include(ch => ch.Attributes)
            .Include(ch => ch.Client)
            .FirstOrDefaultAsync(ch => ch.Id == input.ClientChannelId && ch.Client.CompanyId == CompanyId.Value, ct);

        if (channel == null)
            throw new GraphQLException("Канал клиента не найден.");

        // Update base attributes
        if (input.Name != null) channel.Name = input.Name;
        if (input.LastName != null) channel.LastName = input.LastName;
        if (input.Username != null) channel.Username = input.Username;
        if (input.Phone != null) channel.Phone = input.Phone;
        if (input.Email != null) channel.Email = input.Email;

        // Handle custom attributes — replace entire set
        channel.Attributes.Clear();

        if (input.CustomAttributes is { Count: > 0 })
        {
            foreach (var attr in input.CustomAttributes)
            {
                if (string.IsNullOrWhiteSpace(attr.Key)) continue;
                channel.Attributes.Add(new ClientAttribute
                {
                    Key = attr.Key.Trim(),
                    Value = attr.Value ?? ""
                });
            }
        }

        await context.SaveChangesAsync(ct);

        // Reload to get clean state
        await context.Entry(channel).Collection(ch => ch.Attributes).LoadAsync(ct);
        return channel;
    }
}

public record UpdateClientInput(Guid ClientId, string DisplayName);
public record OkPayload(string Payload);

public record AttributeInput(string Key, string? Value);
public record SetClientChannelAttributesInput(
    Guid ClientChannelId,
    string? Name,
    string? LastName,
    string? Username,
    string? Phone,
    string? Email,
    List<AttributeInput>? CustomAttributes);
