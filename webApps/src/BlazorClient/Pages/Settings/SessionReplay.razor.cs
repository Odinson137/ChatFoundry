using Blazor.Diagrams;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.PathGenerators;
using Blazor.Diagrams.Core.Routers;
using Blazor.Diagrams.Options;
using BlazorClient.Components;
using BlazorClient.Models;
using BlazorClient.Models.Diagram;
using BlazorClient.Models.DTO;
using Microsoft.AspNetCore.Components;

namespace BlazorClient.Pages.Settings;

public interface IReplayDataProvider
{
    NodeStats? GetStats(Guid nodeId);
    bool IsCurrentNode(Guid nodeId);
    bool IsSelectedStep(Guid nodeId);
}

public partial class SessionReplay : IDisposable, IReplayDataProvider
{
    private sealed record SessionVariableItem(string DisplayName, string Value);

    [Parameter] public Guid SessionId { get; set; }

    private BlazorDiagram? Diagram { get; set; }
    private SessionDto? _session;
    private List<SessionActionDto>? _orderedActions;
    private Dictionary<Guid, NodeStats>? _nodeStats;
    private Dictionary<Guid, string> _nodeLabels = new();
    private Dictionary<string, string> _variables = new();

    private bool _isLoading = true;
    private string? _loadError;
    private int _activeTab;
    private int _selectedStepIndex = -1;
    private string _varSearch = "";

    private double _zoomLevel = 1.0;
    private int ZoomPercent => (int)Math.Round(_zoomLevel * 100);

    protected override async Task OnInitializedAsync()
    {
        InitializeDiagram();
        await LoadData();
    }

    private void InitializeDiagram()
    {
        var options = new BlazorDiagramOptions
        {
            AllowMultiSelection = false,
            Zoom = { Enabled = true },
            Links =
            {
                DefaultRouter = new NormalRouter(),
                DefaultPathGenerator = new SmoothPathGenerator(),
                DefaultColor = "#94a3b8",
                DefaultSelectedColor = "#6366f1"
            }
        };

        Diagram = new BlazorDiagram(options);
        Diagram.RegisterComponent<WorkflowNodeModel, ReplayNodeWidget>();
    }

    private async Task LoadData()
    {
        _isLoading = true;
        _loadError = null;

        try
        {
            _session = await ApiClient.GetSessionByIdAsync(SessionId);
            if (_session == null)
            {
                _loadError = "Сессия не найдена.";
                return;
            }

            _orderedActions = _session.Actions?.OrderBy(a => a.CreatedAt).ToList() ?? [];
            _variables = _session.GetVariablesDict();
            BuildNodeStats();

            if (_session.Workflow != null)
            {
                var schema = SchemaService.Deserialize(
                    _session.Workflow.NodesDefinition,
                    _session.Workflow.EdgesDefinition,
                    _session.Workflow.LayoutDefinition);

                ApplySchemaToDiagram(schema);
                BuildNodeLabels(schema);
            }
            else
            {
                _loadError = "Workflow для этой сессии не найден.";
            }
        }
        catch (Exception ex)
        {
            _loadError = $"Ошибка загрузки: {ex.Message}";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ApplySchemaToDiagram(WorkflowSchema schema)
    {
        if (Diagram == null) return;

        Diagram.Nodes.Clear();
        Diagram.Links.Clear();
        var nodeMap = new Dictionary<Guid, NodeModel>();

        foreach (var nDef in schema.Nodes)
        {
            var layout = schema.Layout.FirstOrDefault(l => l.NodeId == nDef.Id);
            var position = layout != null ? new Point(layout.X, layout.Y) : new Point(50, 50);
            var node = CreateNodeInstance(nDef.Type, nDef.Label, position, nDef.Id, nDef.Data);
            nodeMap[nDef.Id] = node;
            Diagram.Nodes.Add(node);
        }

        foreach (var eDef in schema.Edges)
        {
            if (nodeMap.TryGetValue(eDef.From, out var source) && nodeMap.TryGetValue(eDef.To, out var target))
            {
                var sourcePort = source.Ports.FirstOrDefault(p => p.Alignment == PortAlignment.Right) ?? source.Ports.FirstOrDefault();
                var targetPort = target.Ports.FirstOrDefault(p => p.Alignment == PortAlignment.Left) ?? target.Ports.FirstOrDefault();

                if (sourcePort != null && targetPort != null)
                {
                    var link = new WorkflowLinkModel(new SinglePortAnchor(sourcePort), new SinglePortAnchor(targetPort))
                    {
                        Condition = eDef.Condition,
                        Label = eDef.Label
                    };
                    Diagram.Links.Add(link);
                }
            }
        }

        foreach (var node in Diagram.Nodes)
            node.Locked = true;
        foreach (var link in Diagram.Links)
            link.Locked = true;
    }

    private NodeModel CreateNodeInstance(string type, string label, Point position, Guid? id = null, NodeData? data = null)
    {
        data ??= type.ToLower() switch
        {
            "message" => new MessageNodeData { Text = "" },
            "ask" => new AskNodeData { Text = "" },
            "setattribute" => new SetAttributeNodeData { Attribute = "", Value = "" },
            "media" => new MediaNodeData { SourceType = MediaSourceType.Attachment },
            _ => new EmptyNodeData()
        };

        var node = new WorkflowNodeModel(position, type, label, id, data);

        switch (type.ToLower())
        {
            case "start":
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Right));
                break;
            case "end":
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Left));
                break;
            case "condition":
            case "aifilter":
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Left));
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Right));
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Right));
                break;
            default:
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Left));
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Right));
                break;
        }

        return node;
    }

    private void BuildNodeLabels(WorkflowSchema schema)
    {
        _nodeLabels = schema.Nodes.ToDictionary(n => n.Id, n => n.Label);
    }

    private void BuildNodeStats()
    {
        _nodeStats = new Dictionary<Guid, NodeStats>();
        if (_orderedActions == null) return;

        foreach (var action in _orderedActions)
        {
            if (!_nodeStats.TryGetValue(action.NodeId, out var stats))
            {
                stats = new NodeStats();
                _nodeStats[action.NodeId] = stats;
            }

            stats.Visits++;
            switch (action.Status?.ToUpperInvariant())
            {
                case "COMPLETED":
                    stats.Completed++;
                    break;
                case "FAILED":
                    stats.Failed++;
                    break;
            }
        }
    }

    
    public NodeStats? GetStats(Guid nodeId) => _nodeStats?.GetValueOrDefault(nodeId);

    public bool IsCurrentNode(Guid nodeId) => _session?.CurrentNodeId == nodeId;

    public bool IsSelectedStep(Guid nodeId) =>
        _selectedStepIndex >= 0
        && _orderedActions != null
        && _selectedStepIndex < _orderedActions.Count
        && _orderedActions[_selectedStepIndex].NodeId == nodeId;

    private string GetNodeLabel(Guid nodeId)
    {
        return _nodeLabels.TryGetValue(nodeId, out var label) ? label : nodeId.ToString()[..8];
    }

    private void SelectStep(int index)
    {
        _selectedStepIndex = _selectedStepIndex == index ? -1 : index;
        StateHasChanged();
    }

    private void ZoomIn()
    {
        if (Diagram == null) return;
        _zoomLevel = Math.Min(_zoomLevel + 0.1, 3.0);
        Diagram.SetZoom(_zoomLevel);
    }

    private void ZoomOut()
    {
        if (Diagram == null) return;
        _zoomLevel = Math.Max(_zoomLevel - 0.1, 0.2);
        Diagram.SetZoom(_zoomLevel);
    }

    private void GoBack()
    {
        Navigation.NavigateTo("/settings/sessions");
    }

    private IEnumerable<KeyValuePair<string, string>> GetFilteredVariables()
    {
        if (_variables.Count == 0) return [];
        if (string.IsNullOrWhiteSpace(_varSearch))
            return _variables;
        return _variables.Where(kv =>
            kv.Key.Contains(_varSearch, StringComparison.OrdinalIgnoreCase) ||
            kv.Value.Contains(_varSearch, StringComparison.OrdinalIgnoreCase));
    }

    private List<KeyValuePair<string, string>> GetGlobalVariables()
        => GetFilteredVariables().Where(kv => kv.Key.StartsWith("$global.")).ToList();

    private List<KeyValuePair<string, string>> GetSessionVariables()
        => GetFilteredVariables().Where(kv => !kv.Key.StartsWith("$global.")).ToList();

    private List<SessionVariableItem> GetSessionVariableItems()
    {
        var variables = GetSessionVariables();
        var items = new List<SessionVariableItem>();
        var consumedKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var kv in variables)
        {
            if (consumedKeys.Contains(kv.Key))
                continue;

            if (TryParseNodeVariableKey(kv.Key, out var nodeId, out var suffix)
                && suffix.Equals("error", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(kv.Value))
            {
                continue;
            }

            if (TryParseNodeVariableKey(kv.Key, out nodeId, out suffix)
                && (suffix.Equals("output", StringComparison.OrdinalIgnoreCase)
                    || suffix.Equals("messageKind", StringComparison.OrdinalIgnoreCase)))
            {
                var partnerSuffix = suffix.Equals("output", StringComparison.OrdinalIgnoreCase) ? "messageKind" : "output";
                var partnerKey = $"$node.{nodeId}.{partnerSuffix}";
                var partner = variables.FirstOrDefault(v => string.Equals(v.Key, partnerKey, StringComparison.Ordinal));

                if (!string.IsNullOrEmpty(partner.Key) && !consumedKeys.Contains(partner.Key))
                {
                    var outputValue = suffix.Equals("output", StringComparison.OrdinalIgnoreCase) ? kv.Value : partner.Value;
                    var messageKindValue = suffix.Equals("messageKind", StringComparison.OrdinalIgnoreCase) ? kv.Value : partner.Value;

                    items.Add(new SessionVariableItem(
                        GetNodeLabel(nodeId),
                        $"Вывод: {DisplayVariableValue(outputValue)}\nТип сообщения: {DisplayMessageKindValue(messageKindValue)}"));

                    consumedKeys.Add(kv.Key);
                    consumedKeys.Add(partner.Key);
                    continue;
                }
            }

            items.Add(new SessionVariableItem(GetVariableDisplayName(kv.Key), DisplayVariableValue(kv.Value)));
            consumedKeys.Add(kv.Key);
        }

        return items;
    }

    /// <summary>Для переменных $node.{guid}.suffix возвращает "Название блока · suffix", иначе ключ как есть.</summary>
    private string GetVariableDisplayName(string key)
    {
        const string nodePrefix = "$node.";
        if (string.IsNullOrEmpty(key) || !key.StartsWith(nodePrefix, StringComparison.OrdinalIgnoreCase))
            return key;
        var after = key.Substring(nodePrefix.Length);
        var dot = after.IndexOf('.');
        if (dot <= 0)
            return key;
        var guidStr = after.Substring(0, dot);
        var suffix = after.Substring(dot + 1);
        if (!Guid.TryParse(guidStr, out var nodeId))
            return key;
        var label = _nodeLabels.TryGetValue(nodeId, out var l) ? l : nodeId.ToString("N")[..8];
        return $"{label} · {suffix}";
    }

    private static bool TryParseNodeVariableKey(string key, out Guid nodeId, out string suffix)
    {
        nodeId = Guid.Empty;
        suffix = string.Empty;

        const string nodePrefix = "$node.";
        if (string.IsNullOrEmpty(key) || !key.StartsWith(nodePrefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var after = key.Substring(nodePrefix.Length);
        var dot = after.IndexOf('.');
        if (dot <= 0 || dot == after.Length - 1)
            return false;

        var guidStr = after.Substring(0, dot);
        if (!Guid.TryParse(guidStr, out nodeId))
            return false;

        suffix = after.Substring(dot + 1);
        return true;
    }

    private static string DisplayVariableValue(string? value)
        => string.IsNullOrWhiteSpace(value) ? "(пусто)" : value;

    private static string DisplayMessageKindValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "(пусто)";

        return value.Trim().ToUpperInvariant() switch
        {
            "TEXT" => "Текст",
            "IMAGE" => "Изображение",
            "VIDEO" => "Видео",
            "AUDIO" => "Аудио",
            "VOICE" => "Голос",
            "FILE" => "Файл",
            "DOCUMENT" => "Документ",
            "STICKER" => "Стикер",
            "LOCATION" => "Локация",
            "CONTACT" => "Контакт",
            "BUTTON" => "Кнопка",
            _ => value
        };
    }

    private static string FormatChannel(string? channel) => channel?.ToUpperInvariant() switch
    {
        "TELEGRAM" => "Telegram",
        "WEB" => "Web",
        "WHATSAPP" => "WhatsApp",
        "API" => "API",
        "EMAIL" => "Email",
        _ => channel ?? "—"
    };

    private static string StatusBadgeClass(string? status) => status?.ToUpperInvariant() switch
    {
        "ACTIVE" => "bg-primary",
        "COMPLETED" => "bg-success",
        "FAILED" => "bg-danger",
        "CANCELLED" => "bg-warning text-dark",
        _ => "bg-secondary"
    };

    private static string StatusLabel(string? status) => status?.ToUpperInvariant() switch
    {
        "ACTIVE" => "Активна",
        "COMPLETED" => "Завершена",
        "FAILED" => "Ошибка",
        "CANCELLED" => "Отменена",
        _ => status ?? "—"
    };

    private static string ActionStatusClass(string? status) => status?.ToUpperInvariant() switch
    {
        "COMPLETED" => "action-completed",
        "FAILED" => "action-failed",
        "PROCESSING" => "action-processing",
        _ => "action-pending"
    };

    private static string ActionStatusLabel(string? status) => status?.ToUpperInvariant() switch
    {
        "COMPLETED" => "OK",
        "FAILED" => "Ошибка",
        "PROCESSING" => "В процессе",
        "PENDING" => "Ожидание",
        _ => status ?? "—"
    };

    public void Dispose()
    {
    }
}

public class NodeStats
{
    public int Visits { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
}
