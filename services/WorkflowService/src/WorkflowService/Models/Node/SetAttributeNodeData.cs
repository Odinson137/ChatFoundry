namespace WorkflowService.Models.Node;

/// <summary>
/// Данные узла «Атрибут» — запись в глобальные атрибуты клиента (сохраняются между сессиями).
/// Атрибуты загружаются при открытии workflow и доступны как переменные $client.*.
/// Записывать в атрибуты можно только через этот блок.
/// </summary>
public sealed class SetAttributeNodeData : NodeData
{
    /// <summary>
    /// Ключ атрибута: name, username, phone, email — или произвольный ключ для кастомного атрибута.
    /// В сессии хранится как $client.{Attribute}.
    /// </summary>
    public string Attribute { get; init; } = null!;

    /// <summary>
    /// Значение. Может содержать шаблоны {{variable}}.
    /// </summary>
    public string Value { get; init; } = null!;
}
