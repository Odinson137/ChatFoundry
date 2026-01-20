using WorkflowService.Entities;
using WorkflowService.Models.Workflow;

namespace WorkflowService.Utils;

public static class WorkflowConditionEvaluator
{
    public static bool Evaluate(WorkflowCondition condition, Session session)
    {
        return condition switch
        {
            EqualsCondition eq => EvaluateEquals(eq, session),
            ContainsCondition ct => EvaluateContains(ct, session),
            AndCondition and => and.Conditions.All(c => Evaluate(c, session)),
            OrCondition or => or.Conditions.Any(c => Evaluate(c, session)),
            _ => throw new NotSupportedException($"Condition {condition.GetType().Name} is not supported")
        };
    }

    private static bool EvaluateContains(ContainsCondition condition, Session session)
    {
        var left = ResolveOperand(condition.Left, session)?.ToString() ?? string.Empty;
        var right = ResolveOperand(condition.Right, session)?.ToString() ?? string.Empty;

        return left.Contains(right, StringComparison.OrdinalIgnoreCase);
    }

    private static bool EvaluateEquals(EqualsCondition condition, Session session)
    {
        var left = ResolveOperand(condition.Left, session);
        var right = ResolveOperand(condition.Right, session);
        return Equals(left, right);
    }

    private static object? ResolveOperand(object? operand, Session session)
    {
        if (operand is string s && s.StartsWith("$"))
        {
            // Теперь это будет работать и для $user_choice, и для $client.name
            return session.GetVariable(s[1..]);
        }
        return operand;
    }
}