namespace WorkflowService.Enums;

public enum WorkflowNodeType
{
    // ===== Control / flow =====
    Start = 0,
    End = 999,
    SubWorkflow = 100,

    // ===== Conversational intents =====
    Message = 1,        // простой текст
    Ask = 2,            // вопрос с ожиданием ответа и кнопками
    Input = 3,

    // ===== Media / rich content =====
    Image = 10,
    Video = 11,
    Audio = 12,
    Voice = 13,
    File = 14,
    Sticker = 15,
    Link = 16,

    // ===== Interaction / system =====
    Command = 50,
}
