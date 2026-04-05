using System.Text.Json;

namespace WorkflowService.Api;

public sealed class GenerateWorkflowFromAiHttpRequest
{
    public string UserPrompt { get; set; } = "";
    public string? Mode { get; set; }
    public JsonElement? CurrentWorkflow { get; set; }
}
