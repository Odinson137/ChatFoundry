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
    HttpRequest,
    AIFilter,
    AIGenerate,
    Media
}