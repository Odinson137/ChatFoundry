namespace WorkflowService.Models.Node;

public enum MediaKind
{
    Image = 0,
    Video = 1,
    Audio = 2,
    File = 3
}

public enum MediaSourceType
{
    Url = 0,
    Attachment = 1
}

public sealed class MediaNodeData : NodeData
{
    public MediaKind MediaKind { get; init; }
    public MediaSourceType SourceType { get; init; }
    public string Value { get; init; } = "";
    public string? Caption { get; init; }
}
