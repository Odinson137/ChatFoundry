using HotChocolate.Data;
using HotChocolate.Types;
using WorkflowService.Entities;

namespace WorkflowService.GraphQL.Types;

public class SessionType : ObjectType<Session>
{
    protected override void Configure(IObjectTypeDescriptor<Session> descriptor)
    {
        descriptor.Ignore(s => s.ClientProfileDirty);
        descriptor.Field(s => s.Variables).IsProjected(false);
    }
}
