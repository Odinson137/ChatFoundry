namespace WorkflowService.Models.Node;

/// <summary>
/// Тип медиа для узла Media (совпадает с фронтом по числовым значениям).
/// </summary>
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
    /// <summary>При Url — ссылка; при Attachment — ключ в файловом хранилище.</summary>
    public string Value { get; init; } = "";
    public string? Caption { get; init; }
}
