using WorkflowService.Actions.Executors;
using WorkflowService.Enums;

namespace WorkflowService.Actions.Factories;

public class ActionExecutorFactory : IActionExecutorFactory
{
    private readonly Dictionary<WorkflowNodeType, IActionExecutor> _executors;

    public ActionExecutorFactory(IEnumerable<IActionExecutor> executors)
    {
        _executors = executors.ToDictionary(x => x.WorkflowNodeType);
    }

    public IActionExecutor Get(WorkflowNodeType workflowNodeType)
    {
        if (!_executors.TryGetValue(workflowNodeType, out var executor))
            throw new NotSupportedException($"Action {workflowNodeType} not supported");

        return executor;
    }
}