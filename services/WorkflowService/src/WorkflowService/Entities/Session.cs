using System.Text.Json;
using System.Text.Json.Nodes;
using System.ComponentModel.DataAnnotations.Schema;
using Shared.Domain.Entities;
using Shared.Domain.Enums;

namespace WorkflowService.Entities;

public class Session : EntityBase
{
    public Guid WorkflowId { get; set; }
    public BotWorkflow Workflow { get; set; } = null!;

    public string ClientId { get; set; } = null!;
    public DefaultChannel Channel { get; set; }

    public Guid? CurrentNodeId { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.Active;

    public DateTime? CompletedAt { get; set; }
    
    [NotMapped]
    public bool UserProfileDirty { get; set; }

    public List<ActionEntity> Actions { get; set; } = [];
    
    public Dictionary<string, string> Variables { get; set; } = new();

    public void SetVariable(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be empty", nameof(key));
        
        if (key.StartsWith("user."))
        {
            UserProfileDirty = true;
        }

        var stringValue = value?.ToString() ?? string.Empty;
        Variables[key] = stringValue;
    }

    public string? GetVariable(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Variable key cannot be empty", nameof(key));

        return Variables.GetValueOrDefault(key);
    }
    
    public void MoveTo(Guid nextNodeId)
    {
        CurrentNodeId = nextNodeId;
    }
}

