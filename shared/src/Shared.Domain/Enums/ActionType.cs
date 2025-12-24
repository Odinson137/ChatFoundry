namespace Shared.Domain.Enums;

public enum ActionType
{
    // технические
    None = 0,
    Start = 1,
    End = 2,

    // коммуникация
    WaitInput = 10,
    SendMessage = 11,
    SendMedia = 12,
    SendKeyboard = 13,

    // логика
    Condition = 20,
    Switch = 21,
    Delay = 22,

    // интеграции
    HttpRequest = 30,
    PublishKafkaEvent = 31,
    CallWebhook = 32,

    // управление сессией
    SetContext = 40,
    ClearContext = 41,
    CompleteSession = 42,
    FailSession = 43
}
