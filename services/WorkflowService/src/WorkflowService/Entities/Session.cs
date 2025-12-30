using System.Text.Json;
using System.Text.Json.Nodes;
using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace WorkflowService.Entities;

public class Session : EntityBase
{
    public Guid WorkflowId { get; set; }
    public BotWorkflow Workflow { get; set; } = null!;

    public string ClientId { get; set; } = null!;
    public DefaultChannel Channel { get; set; }

    // Убрать потом TODO
    public Guid? CurrentNodeId { get; set; }
    //public Guid? CurrentActionId { get; set; }
    //public ActionEntity? CurrentAction { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.Active;

    public DateTime? CompletedAt { get; set; }

    public List<ActionEntity> Actions { get; set; } = [];

    // TODO потом сделать отдельную таблицу для их хранения. Возможно даже событичную бд взять (так как всё время данные будут только добавляться)
    public string VariablesJson { get; private set; } = "{}";
    
    public void SetVariable(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be empty", nameof(key));

        var json = string.IsNullOrWhiteSpace(VariablesJson)
            ? new JsonObject()
            : JsonNode.Parse(VariablesJson)?.AsObject()
              ?? new JsonObject();

        json[key] = value switch
        {
            null => null,
            JsonNode node => node,
            _ => JsonValue.Create(value)
        };

        VariablesJson = json.ToJsonString();
    }

    public string? GetVariable(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be empty", nameof(key));

        if (string.IsNullOrWhiteSpace(VariablesJson))
            return null;

        var json = JsonNode.Parse(VariablesJson)?.AsObject();
        if (json == null || !json.TryGetPropertyValue(key, out var node) || node == null)
            return null;

        try
        {
            return node.ToString();
        }
        catch (JsonException)
        {
            return null;
        }
    }
    
    public void MoveTo(Guid nextNodeId)
    {
        CurrentNodeId = nextNodeId;
    }
}

