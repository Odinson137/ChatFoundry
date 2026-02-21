namespace BlazorClient.Models;

public enum NodeType
{
    Start,
    End,
    Message,
    Ask,
    Condition,
    Wait,
    SetVariable,
    SetAttribute,
    HttpRequest,
    AIFilter,
    AIGenerate,
    Media
}