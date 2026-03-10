using System.Globalization;
using System.Text.RegularExpressions;
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
            NotEqualsCondition ne => !EvaluateEquals(ne, session, variableService),
            ContainsCondition ct => EvaluateContains(ct, session, variableService),
            GreaterThanCondition gt => Compare(gt, session, variableService) > 0,
            LessThanCondition lt => Compare(lt, session, variableService) < 0,
            GreaterOrEqualCondition ge => Compare(ge, session, variableService) >= 0,
            LessOrEqualCondition le => Compare(le, session, variableService) <= 0,
            StartsWithCondition sw => EvaluateStartsWith(sw, session, variableService),
            EndsWithCondition ew => EvaluateEndsWith(ew, session, variableService),
            RegexMatchCondition rx => EvaluateRegex(rx, session, variableService),
            InListCondition il => EvaluateInList(il, session, variableService),
            IsEmptyCondition ie => EvaluateIsEmpty(ie, session, variableService),
            IsNotEmptyCondition ine => !EvaluateIsEmpty(ine, session, variableService),
            NotCondition not => !Evaluate(not.Condition, session, variableService),
            AndCondition and => and.Conditions.All(c => Evaluate(c, session, variableService)),
            OrCondition or => or.Conditions.Any(c => Evaluate(c, session, variableService)),
            _ => throw new NotSupportedException($"Condition {condition.GetType().Name} is not supported")
        };
    }

    private static bool EvaluateEquals(BinaryCondition condition, Session session, IVariableService variableService)
    {
        var left = ResolveOperand(condition.Left, session, variableService);
        var right = ResolveOperand(condition.Right, session, variableService);
        var ignoreCase = condition.IgnoreCase ?? false;
        if (ignoreCase && left is string ls && right is string rs)
            return string.Equals(ls, rs, StringComparison.OrdinalIgnoreCase);
        return Equals(left, right);
    }

    private static bool EvaluateContains(ContainsCondition condition, Session session, IVariableService variableService)
    {
        var left = ResolveOperand(condition.Left, session, variableService)?.ToString() ?? string.Empty;
        var right = ResolveOperand(condition.Right, session, variableService)?.ToString() ?? string.Empty;
        var ignoreCase = condition.IgnoreCase ?? true;
        return left.Contains(right, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private static bool EvaluateStartsWith(StartsWithCondition condition, Session session, IVariableService variableService)
    {
        var left = ResolveOperand(condition.Left, session, variableService)?.ToString() ?? string.Empty;
        var right = ResolveOperand(condition.Right, session, variableService)?.ToString() ?? string.Empty;
        var ignoreCase = condition.IgnoreCase ?? true;
        return left.StartsWith(right, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private static bool EvaluateEndsWith(EndsWithCondition condition, Session session, IVariableService variableService)
    {
        var left = ResolveOperand(condition.Left, session, variableService)?.ToString() ?? string.Empty;
        var right = ResolveOperand(condition.Right, session, variableService)?.ToString() ?? string.Empty;
        var ignoreCase = condition.IgnoreCase ?? true;
        return left.EndsWith(right, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
    }

    private static bool EvaluateRegex(RegexMatchCondition condition, Session session, IVariableService variableService)
    {
        var left = ResolveOperand(condition.Left, session, variableService)?.ToString() ?? string.Empty;
        var pattern = ResolveOperand(condition.Right, session, variableService)?.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(pattern)) return false;
        var ignoreCase = condition.IgnoreCase ?? false;
        var options = RegexOptions.CultureInvariant | (ignoreCase ? RegexOptions.IgnoreCase : 0);
        try
        {
            return Regex.IsMatch(left, pattern, options);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool EvaluateInList(InListCondition condition, Session session, IVariableService variableService)
    {
        var left = ResolveOperand(condition.Left, session, variableService)?.ToString() ?? string.Empty;
        var right = ResolveOperand(condition.Right, session, variableService)?.ToString() ?? string.Empty;
        var list = right.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var ignoreCase = condition.IgnoreCase ?? true;
        return list.Any(item => string.Equals(left, item, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
    }

    private static bool EvaluateIsEmpty(UnaryCondition condition, Session session, IVariableService variableService)
    {
        var value = ResolveOperand(condition.Left, session, variableService);
        if (value == null) return true;
        var s = value.ToString();
        return string.IsNullOrWhiteSpace(s);
    }

    /// <summary>Сравнение с поддержкой чисел и строк. Числа сравниваются по величине, иначе — по строке.</summary>
    private static int Compare(BinaryCondition condition, Session session, IVariableService variableService)
    {
        var left = ResolveOperand(condition.Left, session, variableService);
        var right = ResolveOperand(condition.Right, session, variableService);
        if (TryAsNumber(left, out var lNum) && TryAsNumber(right, out var rNum))
            return lNum.CompareTo(rNum);
        var ls = left?.ToString() ?? string.Empty;
        var rs = right?.ToString() ?? string.Empty;
        var ignoreCase = condition.IgnoreCase ?? false;
        return string.Compare(ls, rs, CultureInfo.InvariantCulture, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.Ordinal);
    }

    private static bool TryAsNumber(object? value, out decimal num)
    {
        num = 0;
        if (value == null) return false;
        if (value is decimal d) { num = d; return true; }
        if (value is int i) { num = i; return true; }
        if (value is long l) { num = l; return true; }
        if (value is double db) { num = (decimal)db; return true; }
        if (value is float f) { num = (decimal)f; return true; }
        var s = value.ToString()?.Trim();
        if (string.IsNullOrEmpty(s)) return false;
        return decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out num);
    }

    private static object? ResolveOperand(object? operand, Session session, IVariableService variableService)
    {
        if (operand is not string s || string.IsNullOrEmpty(s))
            return operand;

        // Один переменный токен: $key или {{key}} без другого текста — возвращаем значение как есть (для чисел и т.д.)
        var singleVarKey = GetSingleVariableKey(s);
        if (singleVarKey != null)
            return variableService.GetVariable(session, singleVarKey);

        // Иначе собираем строку: подставляем все {{name}} и $key
        return ResolveTemplate(s, session, variableService);
    }

    /// <summary>Если строка — один токен переменной ($key или {{key}}), возвращает key; иначе null.</summary>
    private static string? GetSingleVariableKey(string s)
    {
        s = s.Trim();
        if (s.StartsWith("$", StringComparison.Ordinal) && s.Length > 1 && !s[1..].Contains('$') && !s.Contains("{{", StringComparison.Ordinal))
            return s[1..].Trim();
        var m = SingleBracedVarRegex.Match(s);
        if (m.Success && m.Length == s.Length)
            return m.Groups["name"].Value.Trim();
        return null;
    }

    private static string ResolveTemplate(string text, Session session, IVariableService variableService)
    {
        if (string.IsNullOrEmpty(text)) return text;
        text = BracedVarRegex.Replace(text, match =>
        {
            var name = match.Groups["name"].Value.Trim();
            var value = variableService.GetVariable(session, name);
            return value ?? string.Empty;
        });
        text = DollarVarRegex.Replace(text, match =>
        {
            var key = match.Groups["key"].Value.Trim();
            var value = variableService.GetVariable(session, key);
            return value ?? string.Empty;
        });
        return text;
    }

    private static readonly Regex BracedVarRegex = new(@"\{\{(?<name>[\$a-zA-Z0-9_.-]+)\}\}", RegexOptions.Compiled);
    private static readonly Regex DollarVarRegex = new(@"\$(?<key>[a-zA-Z0-9_.]+)", RegexOptions.Compiled);
    private static readonly Regex SingleBracedVarRegex = new(@"^\s*\{\{(?<name>[\$a-zA-Z0-9_.-]+)\}\}\s*$", RegexOptions.Compiled);
}
