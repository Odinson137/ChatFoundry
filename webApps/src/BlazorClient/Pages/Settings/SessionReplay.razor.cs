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
using Microsoft.Extensions.Localization;

namespace BlazorClient.Pages.Settings;

public interface IReplayDataProvider
{
    NodeStats? GetStats(Guid nodeId);
    bool IsCurrentNode(Guid nodeId);
    bool IsSelectedStep(Guid nodeId);
}

public partial class SessionReplay : IDisposable, IReplayDataProvider
{
    [Inject] private IStringLocalizer<SessionReplay> LReplay { get; set; } = null!;

    private sealed record VariableLine(string Label, string FullValue, string DisplayValue);

    private sealed record SessionVariableItem(string DisplayName, List<VariableLine> Lines, Guid? NodeId = null);

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
    private HashSet<Guid> _expandedNodes = new();
    private string? _modalValue;
    private string? _modalLabel;
    private bool _isCompleting;

    private bool IsSessionActive => _session != null &&
                                    !string.Equals(_session.Status, "COMPLETED", StringComparison.OrdinalIgnoreCase) &&
                                    !string.Equals(_session.Status, "FAILED", StringComparison.OrdinalIgnoreCase) &&
                                    !string.Equals(_session.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase);

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
                _loadError = LReplay["SessionNotFound"];
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
                _loadError = LReplay["WorkflowNotFound"];
            }
        }
        catch (Exception ex)
        {
            _loadError = string.Format(LReplay["LoadError"], ex.Message);
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

    private async Task CompleteSession()
    {
        if (_session == null || _isCompleting) return;

        _isCompleting = true;
        StateHasChanged();

        try
        {
            var success = await ApiClient.CompleteSessionAsync(_session.Id);
            if (success)
            {
                await LoadData();
            }
            else
            {
                _loadError = LReplay["CompleteFailed"];
            }
        }
        catch (Exception ex)
        {
            _loadError = string.Format(LReplay["CompleteError"], ex.Message);
        }
        finally
        {
            _isCompleting = false;
            StateHasChanged();
        }
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

        var nodeGroups = new Dictionary<Guid, List<KeyValuePair<string, string>>>();
        var nonNodeVariables = new List<KeyValuePair<string, string>>();

        foreach (var kv in variables)
        {
            if (TryParseNodeVariableKey(kv.Key, out var nodeId, out var suffix))
            {
                if (suffix.Equals("error", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(kv.Value))
                    continue;

                if (!nodeGroups.ContainsKey(nodeId))
                    nodeGroups[nodeId] = new List<KeyValuePair<string, string>>();
                nodeGroups[nodeId].Add(new KeyValuePair<string, string>(suffix, kv.Value));
            }
            else
            {
                nonNodeVariables.Add(kv);
            }
        }

        foreach (var (nodeId, vars) in nodeGroups)
        {
            var label = GetNodeLabel(nodeId);
            var lines = new List<VariableLine>();

            var outputVar = vars.FirstOrDefault(v => v.Key.Equals("output", StringComparison.OrdinalIgnoreCase));
            var messageKindVar = vars.FirstOrDefault(v => v.Key.Equals("messageKind", StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(outputVar.Key))
            {
                lines.Add(BuildLine(LReplay["SuffixOutput"], outputVar.Value));
                if (!string.IsNullOrEmpty(messageKindVar.Key))
                    lines.Add(new VariableLine(LReplay["SuffixMessageKind"], messageKindVar.Value, DisplayMessageKindValue(messageKindVar.Value)));

                foreach (var v in vars.Where(v =>
                    !v.Key.Equals("output", StringComparison.OrdinalIgnoreCase) &&
                    !v.Key.Equals("messageKind", StringComparison.OrdinalIgnoreCase)))
                {
                    lines.Add(BuildLine(TranslateSuffix(v.Key), v.Value));
                }
            }
            else
            {
                foreach (var v in vars)
                {
                    if (v.Key.Equals("messageKind", StringComparison.OrdinalIgnoreCase))
                        lines.Add(new VariableLine(LReplay["SuffixMessageKind"], v.Value, DisplayMessageKindValue(v.Value)));
                    else
                        lines.Add(BuildLine(TranslateSuffix(v.Key), v.Value));
                }
            }

            if (lines.Count > 0)
                items.Add(new SessionVariableItem(label, lines, nodeId));
        }

        foreach (var kv in nonNodeVariables)
        {
            var line = BuildLine(GetVariableDisplayName(kv.Key), kv.Value);
            items.Add(new SessionVariableItem(line.Label, [line]));
        }

        return items;
    }

    private VariableLine BuildLine(string label, string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return new VariableLine(label, "", LReplay["EmptyValue"]);

        var trimmed = rawValue.Trim();

        if (trimmed.Equals("true", StringComparison.OrdinalIgnoreCase))
            return new VariableLine(label, rawValue, LReplay["Yes"]);
        if (trimmed.Equals("false", StringComparison.OrdinalIgnoreCase))
            return new VariableLine(label, rawValue, LReplay["No"]);

        if (IsJson(trimmed))
            return new VariableLine(label, rawValue, FormatJson(trimmed));

        return new VariableLine(label, rawValue, trimmed);
    }

    private static bool IsJson(string value)
    {
        var t = value.Trim();
        return (t.StartsWith("{") && t.EndsWith("}")) || (t.StartsWith("[") && t.EndsWith("]"));
    }

    private static string FormatJson(string value)
    {
        try
        {
            var parsed = System.Text.Json.JsonDocument.Parse(value.Trim());
            return System.Text.Json.JsonSerializer.Serialize(parsed.RootElement, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return value;
        }
    }

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

    private string TranslateSuffix(string suffix) => suffix.ToLowerInvariant() switch
    {
        "statuscode" => LReplay["SuffixStatusCode"],
        "status" => LReplay["SuffixStatus"],
        "success" => LReplay["SuffixSuccess"],
        "error" => LReplay["SuffixError"],
        "output" => LReplay["SuffixOutput"],
        "messagekind" => LReplay["SuffixMessageKind"],
        "response" => LReplay["SuffixResponse"],
        "request" => LReplay["SuffixRequest"],
        "url" => LReplay["SuffixUrl"],
        "duration" => LReplay["SuffixDuration"],
        "input" => LReplay["SuffixInput"],
        "result" => LReplay["SuffixResult"],
        "data" => LReplay["SuffixData"],
        "headers" => LReplay["SuffixHeaders"],
        "body" => LReplay["SuffixBody"],
        _ => suffix
    };

    private string DisplayVariableValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return LReplay["EmptyValue"];
        var trimmed = value.Trim();
        if (trimmed.Equals("true", StringComparison.OrdinalIgnoreCase)) return LReplay["Yes"];
        if (trimmed.Equals("false", StringComparison.OrdinalIgnoreCase)) return LReplay["No"];
        return IsJson(trimmed) ? FormatJson(trimmed) : trimmed;
    }

    private void OpenValueModal(string label, string fullValue)
    {
        _modalLabel = label;
        _modalValue = IsJson(fullValue) ? FormatJson(fullValue) : fullValue;
        StateHasChanged();
    }

    private void CloseValueModal()
    {
        _modalValue = null;
        _modalLabel = null;
        StateHasChanged();
    }

    private string GetNodeColorClass(Guid? nodeId)
    {
        if (nodeId == null) return "";
        if (_nodeStats == null || !_nodeStats.TryGetValue(nodeId.Value, out var stats)) return "";

        if (stats.Failed > 0) return "var-node-failed";
        if (stats.Completed > 0 && stats.Failed == 0) return "var-node-completed";
        return "var-node-current";
    }

    private void ToggleNode(Guid nodeId)
    {
        if (!_expandedNodes.Remove(nodeId))
            _expandedNodes.Add(nodeId);
        StateHasChanged();
    }

    private string DisplayMessageKindValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return LReplay["EmptyValue"];

        return value.Trim().ToUpperInvariant() switch
        {
            "TEXT" => LReplay["MsgKindText"],
            "IMAGE" => LReplay["MsgKindImage"],
            "VIDEO" => LReplay["MsgKindVideo"],
            "AUDIO" => LReplay["MsgKindAudio"],
            "VOICE" => LReplay["MsgKindVoice"],
            "FILE" => LReplay["MsgKindFile"],
            "DOCUMENT" => LReplay["MsgKindDocument"],
            "STICKER" => LReplay["MsgKindSticker"],
            "LOCATION" => LReplay["MsgKindLocation"],
            "CONTACT" => LReplay["MsgKindContact"],
            "BUTTON" => LReplay["MsgKindButton"],
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

    private string StatusLabel(string? status) => status?.ToUpperInvariant() switch
    {
        "ACTIVE" => LReplay["StatusActive"],
        "COMPLETED" => LReplay["StatusCompleted"],
        "FAILED" => LReplay["StatusFailed"],
        "CANCELLED" => LReplay["StatusCancelled"],
        _ => status ?? "—"
    };

    private static string ActionStatusClass(string? status) => status?.ToUpperInvariant() switch
    {
        "COMPLETED" => "action-completed",
        "FAILED" => "action-failed",
        "PROCESSING" => "action-processing",
        _ => "action-pending"
    };

    private string ActionStatusLabel(string? status) => status?.ToUpperInvariant() switch
    {
        "COMPLETED" => "OK",
        "FAILED" => LReplay["ActionStatusFailed"],
        "PROCESSING" => LReplay["ActionStatusProcessing"],
        "PENDING" => LReplay["ActionStatusPending"],
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
