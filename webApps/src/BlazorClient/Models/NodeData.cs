using System.Text.Json.Serialization;

namespace BlazorClient.Models;

[JsonDerivedType(typeof(EmptyNodeData), "Empty")]
[JsonDerivedType(typeof(MessageNodeData), "Message")]
[JsonDerivedType(typeof(SetVariableNodeData), "SetVariable")]
[JsonDerivedType(typeof(SetAttributeNodeData), "SetAttribute")]
[JsonDerivedType(typeof(AskNodeData), "Ask")]
[JsonDerivedType(typeof(HttpRequestNodeData), "HttpRequest")]
[JsonDerivedType(typeof(AIGenerateNodeData), "AIGenerate")]
[JsonDerivedType(typeof(MediaNodeData), "Media")]
[JsonDerivedType(typeof(SubWorkflowNodeData), "SubWorkflow")]
[JsonDerivedType(typeof(WaitNodeData), "Wait")]
[JsonDerivedType(typeof(TimerStartNodeData), "TimerStart")]
[JsonDerivedType(typeof(WebhookWaitNodeData), "WebhookWait")]

public abstract class NodeData { }

public class EmptyNodeData : NodeData { }

public class HttpRequestNodeData : NodeData
{
    public string Method { get; set; } = "GET";
    public string Url { get; set; } = string.Empty;
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    public bool ContinueOnError { get; set; }
}

public class MessageNodeData : NodeData
{

    public string Text { get; set; } = string.Empty;
}

public class SetVariableNodeData : NodeData
{
    public string Variable { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}

public class SetAttributeNodeData : NodeData
{
    public string Attribute { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}

public class AskButtonData
{
    public string Text { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; set; }
}

public class AskUiData
{
    public List<AskButtonData> Buttons { get; set; } = new();
}

public class AskNodeData : NodeData
{
    public string Text { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AskUiData? Ui { get; set; }
}

public class AIGenerateNodeData : NodeData
{
    public string Prompt { get; set; } = string.Empty;

    public bool IncludeChatContext { get; set; }
    public bool ContinueOnError { get; set; }
}

public enum MediaKind
{
    Image,
    Video,
    Audio,
    File
}

public enum MediaSourceType
{
    Url,
    Attachment
}

public class MediaNodeData : NodeData
{
    public MediaKind MediaKind { get; set; } = MediaKind.Image;

    public MediaSourceType SourceType { get; set; } = MediaSourceType.Url;

    public string Value { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Caption { get; set; }
}

public class SubWorkflowNodeData : NodeData
{
    public Guid WorkflowId { get; set; }

    public Dictionary<string, string> InputMappings { get; set; } = new();

    public Dictionary<string, string> OutputMappings { get; set; } = new();
}

public class WaitNodeData : NodeData
{
    public string Duration { get; set; } = "60";
    public string Unit { get; set; } = "Seconds";
}

public class TimerStartNodeData : NodeData
{
    public string ScheduleType { get; set; } = "OneTime";
    public string? FireTimeUtc { get; set; }
    public string? CronExpression { get; set; }
    public string Timezone { get; set; } = "UTC";
    public ClientFilterCriteria? ClientFilter { get; set; }
}

public class ClientFilterCriteria
{
    public List<string> ClientIds { get; set; } = new();
    public List<ClientAttributeFilterCondition> AttributeConditions { get; set; } = new();
    public string Logic { get; set; } = "and";
    public List<Guid> Channels { get; set; } = new();
}

public class ClientAttributeFilterCondition
{
    public string AttributeKey { get; set; } = "";
    public string Operator { get; set; } = "equals";
    public string Value { get; set; } = "";
    public bool? IgnoreCase { get; set; }
}

public class WebhookWaitNodeData : NodeData
{
    public int TimeoutSeconds { get; set; } = 0;
    public string CallbackUrlTemplate { get; set; } = string.Empty;
}