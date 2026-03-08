using System.ComponentModel.DataAnnotations.Schema;
using Shared.Domain.Entities;
using WorkflowService.Models;
using WorkflowService.Utils;

namespace WorkflowService.Entities;

public class BotWorkflow : EntityBase
{
    public Guid BotId { get; set; }
    public Bot Bot { get; set; } = null!;

    public string NodesDefinition { get; set; } = "[]";
    public string EdgesDefinition { get; set; } = "[]";
    public string LayoutDefinition { get; set; } = "[]";

    public int Version { get; set; } = 1;
    
    public bool IsActiveBotWorkflow { get; set; } = false;

    public string InputParametersDefinition { get; set; } = "[]";
    public string OutputParametersDefinition { get; set; } = "[]";

    [NotMapped]
    public List<WorkflowParameter> InputParameters => WorkflowParameterSerializer.Deserialize(InputParametersDefinition);

    [NotMapped]
    public List<WorkflowParameter> OutputParameters => WorkflowParameterSerializer.Deserialize(OutputParametersDefinition);

    public ICollection<Session> Sessions { get; set; } = [];
}