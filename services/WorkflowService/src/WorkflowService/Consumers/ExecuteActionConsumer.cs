using MassTransit;
using WorkflowService.Actions.Factories;
using WorkflowService.Events;
using WorkflowService.Interfaces;

namespace WorkflowService.Consumers;

public class ExecuteActionConsumer(
    IActionRepository actionRepository,
    IActionExecutorFactory executorFactory,
    ISessionResolver sessionResolver)
    : IConsumer<ExecuteActionCommand>
{
    public async Task Consume(ConsumeContext<ExecuteActionCommand> context)
    {
        var message = context.Message;
        var ct = context.CancellationToken;

        var action = await actionRepository.GetAsync(message.ActionId, ct);

        if (action == null)
            throw new InvalidOperationException($"Action {message.ActionId} not found");

        var executor = executorFactory.Get(action.WorkflowNodeType);

        try
        {
            action.MarkInProgress();
            await actionRepository.SaveAsync(action, ct);

            await executor.ExecuteAsync(action, message, ct);
        }
        catch (Exception ex)
        {
            const int maxErrorLength = 2000;
            action.ErrorMessage = ex.Message.Length <= maxErrorLength
                ? ex.Message
                : ex.Message[..maxErrorLength] + "...";
            action.MarkFailed();
            await actionRepository.SaveAsync(action, ct);
            await sessionResolver.CloseSessionAndHierarchyAsync(action.SessionId, ct);
        }
    }
}