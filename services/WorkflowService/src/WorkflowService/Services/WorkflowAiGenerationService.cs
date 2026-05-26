using System.Text.Json;
using WorkflowValidation;

namespace WorkflowService.Services;

public class WorkflowAiGenerationService(
    IOpenAiService openAi,
    WorkflowAiPromptProvider promptProvider,
    ILogger<WorkflowAiGenerationService> logger)
{
    private const string JsonSuffix =
        "\n\nВерни один JSON-объект с полями nodes, edges, layout (массивы). При необходимости inputParameters и outputParameters. Без markdown, без ```.";

    public async Task<WorkflowAiGenerateResult> GenerateAsync(
        string userPrompt,
        string mode,
        JsonElement? currentWorkflow,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userPrompt))
            return new WorkflowAiGenerateResult(false, null, ["Введите описание бота или доработки."]);

        var instruction = await promptProvider.GetMarkdownAsync(cancellationToken);
        var system = instruction + JsonSuffix;

        var userContent = string.Equals(mode, "merge", StringComparison.OrdinalIgnoreCase) && currentWorkflow.HasValue
            ? BuildMergeContent(userPrompt, currentWorkflow.Value)
            : BuildReplaceContent(userPrompt);

        string raw;
        try
        {
            raw = await openAi.GetJsonObjectCompletionAsync(system, userContent, cancellationToken);
        }
        catch (Exception ex)
        {
            return new WorkflowAiGenerateResult(false, null, [$"Ошибка OpenAI: {ex.Message}"]);
        }

        if (string.IsNullOrWhiteSpace(raw))
            return new WorkflowAiGenerateResult(false, null, ["Пустой ответ от AI. Проверьте ключ OpenAI в конфигурации сервиса."]);

        raw = StripMarkdownCodeFence(raw);

        try
        {
            using var doc = JsonDocument.Parse(raw);
            raw = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions { WriteIndented = false });
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "AI returned invalid JSON");
            return new WorkflowAiGenerateResult(false, null, [$"Некорректный JSON от модели: {ex.Message}"]);
        }

        var validationErrors = WorkflowSchemaValidator.Validate(raw);
        if (validationErrors.Count > 0)
            return new WorkflowAiGenerateResult(false, raw, validationErrors);

        return new WorkflowAiGenerateResult(true, raw, []);
    }

    private static string BuildReplaceContent(string userPrompt)
        => "Задача:\n" + userPrompt.Trim();

    private static string BuildMergeContent(string userPrompt, JsonElement current)
        => "Текущая схема workflow (JSON):\n" + current.GetRawText()
           + "\n\nЗадача пользователя (доработать схему):\n" + userPrompt.Trim()
           + "\n\nВерни полный обновлённый JSON с теми же полями (nodes, edges, layout, ...). Сохрани существующие id узлов, где логика не меняется.";

    private static string StripMarkdownCodeFence(string raw)
    {
        var trimmed = raw.Trim();
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            return trimmed;

        var firstNl = trimmed.IndexOf('\n');
        if (firstNl < 0)
            return trimmed;

        var end = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        if (end <= firstNl)
            return trimmed;

        return trimmed[(firstNl + 1)..end].Trim();
    }
}

public record WorkflowAiGenerateResult(bool Success, string? WorkflowJson, IReadOnlyList<string> Errors);
