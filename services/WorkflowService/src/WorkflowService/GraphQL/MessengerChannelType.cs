using HotChocolate.Types;
using WorkflowService.Entities;

namespace WorkflowService.GraphQL;

public class MessengerChannelType : ObjectType<MessengerChannel>
{
    protected override void Configure(IObjectTypeDescriptor<MessengerChannel> descriptor)
    {
        descriptor.Ignore(c => c.Token);
    }
}
