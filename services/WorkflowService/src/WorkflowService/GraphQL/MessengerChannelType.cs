using HotChocolate.Types;
using WorkflowService.Entities;

namespace WorkflowService.GraphQL;

/// <summary>
/// Явный тип GraphQL для MessengerChannel: скрываем Token, остаётся только MaskedToken.
/// </summary>
public class MessengerChannelType : ObjectType<MessengerChannel>
{
    protected override void Configure(IObjectTypeDescriptor<MessengerChannel> descriptor)
    {
        descriptor.Ignore(c => c.Token);
    }
}
