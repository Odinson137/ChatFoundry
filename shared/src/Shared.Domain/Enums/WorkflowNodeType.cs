namespace Shared.Domain.Enums;

public enum WorkflowNodeType
{
    Start,
    Message,
    Ask, // Is Waitable ? true 
    Input,
    Gateway,
    SubWorkflow,
    End
}