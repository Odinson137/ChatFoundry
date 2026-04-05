using ClientService.Data;
using HotChocolate;
using HotChocolate.Types;
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
}

public record UpdateClientInput(Guid ClientId, string DisplayName);
public record OkPayload(string Payload);

