using HotChocolate.Types;
using WorkflowService.Entities;

namespace WorkflowService.GraphQL.Types;

/// <summary>
/// Exposes only stored columns; parameters are sent as JSON strings (inputParametersDefinition, outputParametersDefinition).
/// </summary>
public class BotWorkflowType : ObjectType<BotWorkflow>
{
    protected override void Configure(IObjectTypeDescriptor<BotWorkflow> descriptor)
    {
        descriptor.Ignore(w => w.InputParameters);
        descriptor.Ignore(w => w.OutputParameters);
    }
}
