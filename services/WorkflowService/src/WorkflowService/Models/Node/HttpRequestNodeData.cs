using System.Collections.Generic;

namespace WorkflowService.Models.Node;

public sealed class HttpRequestNodeData : NodeData, IContinueOnError
{
    public string Method { get; set; } = "GET";

    public string Url { get; set; } = string.Empty;

    public string? Body { get; set; }

    public Dictionary<string, string> Headers { get; set; } = new();

    public bool ContinueOnError { get; set; }
}
