using System.Collections.Generic;

namespace WorkflowService.Models.Node;

public sealed class HttpRequestNodeData : NodeData
{
    /// <summary>
    /// HTTP Method (GET, POST, PUT, DELETE)
    /// </summary>
    public string Method { get; set; } = "GET";

    /// <summary>
    /// Request URL. Can contain variables.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Request Body. Can contain variables.
    /// </summary>
    public string? Body { get; set; }
    
    /// <summary>
    /// Request Headers. Values can contain variables.
    /// </summary>
    public Dictionary<string, string> Headers { get; set; } = new();
}
