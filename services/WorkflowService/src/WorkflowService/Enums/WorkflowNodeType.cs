namespace WorkflowService.Enums;

public enum WorkflowNodeType
{
    
    Start = 0,
    SubWorkflow = 100,

    
    Message = 1,        
    Ask = 2,            
    Input = 3,

    
    Media = 9,   
    Image = 10,
    Video = 11,
    Audio = 12,
    Voice = 13,
    File = 14,
    Sticker = 15,
    Link = 16,

    Condition = 20,     
    Wait = 21,          
    SetAttribute = 24,  
    HttpRequest = 23,   
    
    AIFilter = 30,      
    AIGenerate = 31,
    
    
    Command = 50,

    TransferToOperator = 60,
}
