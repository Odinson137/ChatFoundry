using System.Text.Json.Serialization;

namespace BlazorClient.Models;

// Базовый класс/рекорд для всех специфических данных узлов.
// [JsonDerivedType] помогает System.Text.Json корректно сериализовать/десериализовать
// полиморфные типы (когда NodeData может быть MessageNodeData, AskNodeData и т.д.).
// 'typeDiscriminator' должен соответствовать тому, как сервер определяет тип данных.
[JsonDerivedType(typeof(EmptyNodeData), "Empty")] // 'Empty' - если нет специфичных данных
[JsonDerivedType(typeof(MessageNodeData), "Message")] // 'Message' - для узла сообщения
// Добавьте сюда другие типы NodeData, если они появятся, например:
// [JsonDerivedType(typeof(AskNodeData), "Ask")] 
public abstract class NodeData { }

public class EmptyNodeData : NodeData { }

public class MessageNodeData : NodeData
{
    // Теперь здесь set вместо init, и Blazor сможет привязаться к этому полю
    public string Text { get; set; } = string.Empty;
}
// Если у вас есть другие типы узлов с данными (например, "Ask"), добавьте их здесь:
//public record AskNodeData(string Question, List<string> Options) : NodeData();