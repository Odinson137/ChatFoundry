using System.Text.Json.Serialization;

namespace BlazorClient.Models;

// Базовый класс/рекорд для всех специфических данных узлов.
// [JsonDerivedType] помогает System.Text.Json корректно сериализовать/десериализовать
// полиморфные типы (когда NodeData может быть MessageNodeData, AskNodeData и т.д.).
// 'typeDiscriminator' должен соответствовать тому, как сервер определяет тип данных.
[JsonDerivedType(typeof(EmptyNodeData), "Empty")]
[JsonDerivedType(typeof(MessageNodeData), "Message")]
[JsonDerivedType(typeof(SetVariableNodeData), "SetVariable")]
[JsonDerivedType(typeof(SetAttributeNodeData), "SetAttribute")]
[JsonDerivedType(typeof(AskNodeData), "Ask")]
[JsonDerivedType(typeof(HttpRequestNodeData), "HttpRequest")]
[JsonDerivedType(typeof(AIGenerateNodeData), "AIGenerate")]
[JsonDerivedType(typeof(MediaNodeData), "Media")]
[JsonDerivedType(typeof(SubWorkflowNodeData), "SubWorkflow")]

public abstract class NodeData { }

public class EmptyNodeData : NodeData { }

public class HttpRequestNodeData : NodeData
{
    public string Method { get; set; } = "GET";
    public string Url { get; set; } = string.Empty;
    public string? Body { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new();
}

public class MessageNodeData : NodeData
{
    // Теперь здесь set вместо init, и Blazor сможет привязаться к этому полю
    public string Text { get; set; } = string.Empty;
}

public class SetVariableNodeData : NodeData
{
    /// <summary>
    /// Имя переменной, куда будет сохранено значение (например, "user_name"). Не используйте $client.* — для атрибутов клиента есть блок «Атрибут».
    /// </summary>
    public string Variable { get; set; } = string.Empty;
    
    /// <summary>
    /// Значение, которое будет установлено. Может быть статическим текстом или ссылкой на другую переменную через {{variable_name}}
    /// </summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Данные узла «Атрибут» — запись в глобальные атрибуты клиента (имя, почта, телефон и др.), сохраняются между сессиями.
/// </summary>
public class SetAttributeNodeData : NodeData
{
    /// <summary>
    /// Ключ атрибута: name, username, phone, email — или произвольный ключ для кастомного атрибута.
    /// </summary>
    public string Attribute { get; set; } = string.Empty;
    
    /// <summary>
    /// Значение. Может содержать шаблоны {{variable_name}}.
    /// </summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// Кнопка для блока Ask (inline-кнопка под сообщением).
/// </summary>
public class AskButtonData
{
    /// <summary>Текст на кнопке (отображается пользователю).</summary>
    public string Text { get; set; } = string.Empty;
    /// <summary>Значение, которое попадёт в переменную при нажатии.</summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>
/// UI-данные блока Ask (кнопки и т.д.).
/// </summary>
public class AskUiData
{
    public List<AskButtonData> Buttons { get; set; } = new();
}

public class AskNodeData : NodeData
{
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Кнопки под вопросом (если пусто — кнопки не отправляются).
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AskUiData? Ui { get; set; }
}

public class AIGenerateNodeData : NodeData
{
    public string Prompt { get; set; } = string.Empty;
}

/// <summary>
/// Тип медиа для блока «Медиа».
/// </summary>
public enum MediaKind
{
    Image,
    Video,
    Audio,
    File
}

/// <summary>
/// Источник медиа: ссылка или прикреплённый файл из хранилища.
/// </summary>
public enum MediaSourceType
{
    Url,
    Attachment
}

/// <summary>
/// Данные узла «Медиа»: тип медиа, источник (ссылка или ключ в хранилище), подпись.
/// </summary>
public class MediaNodeData : NodeData
{
    public MediaKind MediaKind { get; set; } = MediaKind.Image;

    public MediaSourceType SourceType { get; set; } = MediaSourceType.Url;

    /// <summary>
    /// При SourceType.Url — прямая ссылка на медиа. При SourceType.Attachment — ID файла в файловом сервисе (или переменная).
    /// </summary>
    public string Value { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Caption { get; set; }
}

public class SubWorkflowNodeData : NodeData
{
    public Guid WorkflowId { get; set; }

    /// <summary>
    /// Child parameter name -> expression with {{parentVar}} templates.
    /// </summary>
    public Dictionary<string, string> InputMappings { get; set; } = new();

    /// <summary>
    /// Parent variable name -> child variable name.
    /// </summary>
    public Dictionary<string, string> OutputMappings { get; set; } = new();
}