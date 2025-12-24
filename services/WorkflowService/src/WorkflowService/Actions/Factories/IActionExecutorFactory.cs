using Shared.Domain.Enums;
using WorkflowService.Actions.Executors;

namespace WorkflowService.Actions.Factories;

public interface IActionExecutorFactory
{
    IActionExecutor Get(WorkflowNodeType actionType);
}