using WorkflowService.Actions.Executors;
using WorkflowService.Enums;

namespace WorkflowService.Actions.Factories;

public interface IActionExecutorFactory
{
    IActionExecutor Get(WorkflowNodeType actionType);
}