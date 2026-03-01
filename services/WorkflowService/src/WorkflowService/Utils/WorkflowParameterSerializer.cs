using System.Text.Json;
using WorkflowService.Models;

namespace WorkflowService.Utils;

public static class WorkflowParameterSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static string Serialize(List<WorkflowParameter> list)
    {
        if (list == null || list.Count == 0)
            return "[]";
        return JsonSerializer.Serialize(list, Options);
    }

    public static List<WorkflowParameter> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]" || json == "{}")
            return [];
        try
        {
            return JsonSerializer.Deserialize<List<WorkflowParameter>>(json, Options) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
