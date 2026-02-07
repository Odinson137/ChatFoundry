using WorkflowService.Entities;
using WorkflowService.Interfaces;
using WorkflowService.Models.Workflow;

namespace WorkflowService.Utils;

public static class WorkflowConditionEvaluator
{
    public static bool Evaluate(WorkflowCondition condition, Session session, IVariableService variableService)
    {
        return condition switch
        {
            EqualsCondition eq => EvaluateEquals(eq, session, variableService),
            ContainsCondition ct => EvaluateContains(ct, session, variableService),
            AndCondition and => and.Conditions.All(c => Evaluate(c, session, variableService)),
            OrCondition or => or.Conditions.Any(c => Evaluate(c, session, variableService)),
            _ => throw new NotSupportedException($"Condition {condition.GetType().Name} is not supported")
        };
    }

    private static bool EvaluateContains(ContainsCondition condition, Session session, IVariableService variableService)
    {
        var left = ResolveOperand(condition.Left, session, variableService)?.ToString() ?? string.Empty;
        var right = ResolveOperand(condition.Right, session, variableService)?.ToString() ?? string.Empty;

        return left.Contains(right, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EvaluateEquals(EqualsCondition condition, Session session, IVariableService variableService)
    {
        var left = ResolveOperand(condition.Left, session, variableService);
        var right = ResolveOperand(condition.Right, session, variableService);
        return Equals(left, right);
    }

    private static object? ResolveOperand(object? operand, Session session, IVariableService variableService)
    {
        if (operand is string s && s.StartsWith("$"))
        {
            return variableService.GetVariable(session, s[1..]);
        }
        return operand;
    }
}
