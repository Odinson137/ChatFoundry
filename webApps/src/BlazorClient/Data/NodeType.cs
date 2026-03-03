namespace BlazorClient.Models;

public enum NodeType
{
    Start,
    End,
    Message,
    Ask,
    Condition,
    Wait,
    SetAttribute,
    HttpRequest,
    AIFilter,
    AIGenerate,
    Media,
    SubWorkflow
}