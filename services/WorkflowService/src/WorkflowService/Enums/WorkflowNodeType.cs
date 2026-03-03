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
    Media = 9,   // один блок «Медиа» с выбором типа внутри (Image/Video/Audio/File)
    Image = 10,
    Video = 11,
    Audio = 12,
    Voice = 13,
    File = 14,
    Sticker = 15,
    Link = 16,

    Condition = 20,     // If-Else ветвление
    Wait = 21,          // Пауза/Задержка
    [Obsolete("SetVariable node type was removed. Use $node.{guid}.output auto-variables instead.")]
    SetVariable = 22,
    SetAttribute = 24,  // Запись в атрибуты клиента (глобальные, между сессиями)
    HttpRequest = 23,   // Внешний API запрос
    
    AIFilter = 30,      // Определение интента/токсичности
    AIGenerate = 31,
    
    // ===== Interaction / system =====
    Command = 50,
}
