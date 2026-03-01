using HotChocolate;
using HotChocolate.Types;
using Shared.Infrastructure.GraphQl;
using WorkflowService.Data;
using WorkflowService.Entities;

namespace WorkflowService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class BotWorkflowMutation
{
    public async Task<AddBotWorkflowPayload> AddBotWorkflowAsync(
        AddBotWorkflowInput input,
        [Service] WorkflowDbContext context)
    {
        var workflow = new BotWorkflow
        {
            BotId = input.BotId,
            NodesDefinition = input.NodesDefinition,
            EdgesDefinition = input.EdgesDefinition,
            LayoutDefinition = input.LayoutDefinition,
            Version = input.Version,
            IsActiveBotWorkflow = input.IsActiveBotWorkflow,
            InputParametersDefinition = input.InputParametersDefinition ?? "[]",
            OutputParametersDefinition = input.OutputParametersDefinition ?? "[]"
        };

        context.Workflows.Add(workflow);
        await context.SaveChangesAsync();

        return new AddBotWorkflowPayload(workflow);
    }

    public async Task<UpdateBotWorkflowPayload> UpdateBotWorkflowAsync(
        UpdateBotWorkflowInput input,
        [Service] WorkflowDbContext context)
    {
        var workflow = await context.Workflows.FindAsync(input.WorkflowId);

        if (workflow is null)
        {
            return new UpdateBotWorkflowPayload(null);
        }

        workflow.NodesDefinition = input.NodesDefinition ?? workflow.NodesDefinition;
        workflow.EdgesDefinition = input.EdgesDefinition ?? workflow.EdgesDefinition;
        workflow.LayoutDefinition = input.LayoutDefinition ?? workflow.LayoutDefinition;
        workflow.Version = input.Version ?? workflow.Version;
        workflow.IsActiveBotWorkflow = input.IsActiveBotWorkflow ?? workflow.IsActiveBotWorkflow;

        if (input.InputParametersDefinition != null)
            workflow.InputParametersDefinition = input.InputParametersDefinition;
        if (input.OutputParametersDefinition != null)
            workflow.OutputParametersDefinition = input.OutputParametersDefinition;

        await context.SaveChangesAsync();

        return new UpdateBotWorkflowPayload(workflow);
    }

    public async Task<DeleteBotWorkflowPayload> DeleteBotWorkflowAsync(
        DeleteBotWorkflowInput input,
        [Service] WorkflowDbContext context)
    {
        var workflow = await context.Workflows.FindAsync(input.WorkflowId);

        if (workflow is null)
        {
            return new DeleteBotWorkflowPayload(null);
        }

        context.Workflows.Remove(workflow);
        await context.SaveChangesAsync();

        return new DeleteBotWorkflowPayload(workflow);
    }
}

#region Records for GraphQL

public record AddBotWorkflowInput(
    Guid BotId, 
    string NodesDefinition, 
    string EdgesDefinition, 
    string LayoutDefinition, 
    int Version = 1, 
    bool IsActiveBotWorkflow = false,
    string? InputParametersDefinition = null,
    string? OutputParametersDefinition = null);

public record AddBotWorkflowPayload(BotWorkflow BotWorkflow);

public record UpdateBotWorkflowInput(
    Guid WorkflowId, 
    string? NodesDefinition, 
    string? EdgesDefinition, 
    string? LayoutDefinition, 
    int? Version, 
    bool? IsActiveBotWorkflow,
    string? InputParametersDefinition = null,
    string? OutputParametersDefinition = null);

public record UpdateBotWorkflowPayload(BotWorkflow? BotWorkflow);

public record DeleteBotWorkflowInput(Guid WorkflowId);

public record DeleteBotWorkflowPayload(BotWorkflow? BotWorkflow);

#endregion
