using System.Text.Json.Serialization;

namespace BlazorClient.Models;

// Базовый класс/рекорд для всех специфических данных узлов.
// [JsonDerivedType] помогает System.Text.Json корректно сериализовать/десериализовать
// полиморфные типы (когда NodeData может быть MessageNodeData, AskNodeData и т.д.).
// 'typeDiscriminator' должен соответствовать тому, как сервер определяет тип данных.
[JsonDerivedType(typeof(EmptyNodeData), "Empty")] // 'Empty' - если нет специфичных данных
[JsonDerivedType(typeof(MessageNodeData), "Message")] // 'Message' - для узла сообщения
[JsonDerivedType(typeof(SetVariableNodeData), "SetVariable")] // 'SetVariable' - для узла установки переменной
[JsonDerivedType(typeof(AskNodeData), "Ask")]
[JsonDerivedType(typeof(HttpRequestNodeData), "HttpRequest")]

public abstract class NodeData { }

public class EmptyNodeData : NodeData { }

public class HttpRequestNodeData : NodeData
{
    public string Method { get; set; } = "GET";
    public string Url { get; set; } = string.Empty;
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
    
    [JsonPropertyName("responseVariable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ResponseVariable { get; set; }
    
    [JsonPropertyName("statusCodeVariable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? StatusCodeVariable { get; set; }
}

public class MessageNodeData : NodeData
{
    // Теперь здесь set вместо init, и Blazor сможет привязаться к этому полю
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Имя переменной, в которую будет сохранен результат (например, ответ пользователя).
    /// </summary>
    [JsonPropertyName("variable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Variable { get; set; }
}

public class SetVariableNodeData : NodeData
{
    /// <summary>
    /// Имя переменной, куда будет сохранено значение (например, "user_name" или "client.name")
    /// </summary>
    public string Variable { get; set; } = string.Empty;
    
    /// <summary>
    /// Значение, которое будет установлено. Может быть статическим текстом или ссылкой на другую переменную через {{variable_name}}
    /// </summary>
    public string Value { get; set; } = string.Empty;
}

public class AskNodeData : NodeData
{
    public string Text { get; set; } = string.Empty;
    
    /// <summary>
    /// Имя переменной, в которую будет сохранен результат (например, ответ пользователя).
    /// </summary>
    [JsonPropertyName("variable")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Variable { get; set; }
}