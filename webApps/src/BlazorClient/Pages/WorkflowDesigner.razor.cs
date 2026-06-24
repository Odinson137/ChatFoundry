using Blazor.Diagrams;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Blazor.Diagrams.Core.PathGenerators;
using Blazor.Diagrams.Core.Routers;
using Blazor.Diagrams.Options;
using BlazorClient.Interfaces;
using BlazorClient.Models;
using BlazorClient.Models.DTO;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;
using BlazorClient.Components;
using BlazorClient.Models.Diagram;

namespace BlazorClient.Pages;

public class VariableInfo
{
    public string Name { get; set; } = "";
    public VariableType Type { get; set; }
    public string? SourceNode { get; set; }
    public List<string> UsageNodes { get; set; } = new();
    public int UsageCount => UsageNodes.Count;
}

public enum VariableType
{
    GlobalAttribute,
    System,
    User,
    Custom
}

public partial class WorkflowDesigner : IDisposable
{
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;
    [Inject] private IWorkflowApiClient ApiClient { get; set; } = null!;
    [Inject] private IWorkflowSchemaService SchemaService { get; set; } = null!;
    [Inject] private IFileApiClient FileApiClient { get; set; } = null!;
    [Inject] private IClientApiClient ClientApiClient { get; set; } = null!;
    [Inject] private IStringLocalizer<WorkflowDesigner> LDesigner { get; set; } = null!;

    private List<ChannelDto> AvailableChannels { get; set; } = [];
    private string UserTimezone { get; set; } = "UTC";
    private Dictionary<string, string> TimezoneOffsets { get; set; } = new();

    private (string Id, string Label)[] CommonTimezones =>
    [
        ("UTC", "UTC"),
        ("Europe/London", LDesigner["TzLondon"]), ("Europe/Paris", LDesigner["TzParis"]), ("Europe/Berlin", LDesigner["TzBerlin"]),
        ("Europe/Madrid", LDesigner["TzMadrid"]), ("Europe/Rome", LDesigner["TzRome"]), ("Europe/Amsterdam", LDesigner["TzAmsterdam"]),
        ("Europe/Brussels", LDesigner["TzBrussels"]), ("Europe/Vienna", LDesigner["TzVienna"]), ("Europe/Warsaw", LDesigner["TzWarsaw"]),
        ("Europe/Prague", LDesigner["TzPrague"]), ("Europe/Budapest", LDesigner["TzBudapest"]), ("Europe/Bucharest", LDesigner["TzBucharest"]),
        ("Europe/Athens", LDesigner["TzAthens"]), ("Europe/Helsinki", LDesigner["TzHelsinki"]),
        ("Europe/Istanbul", LDesigner["TzIstanbul"]), ("Europe/Moscow", LDesigner["TzMoscow"]), ("Europe/Minsk", LDesigner["TzMinsk"]), ("Europe/Kiev", LDesigner["TzKyiv"]),
        ("Asia/Dubai", LDesigner["TzDubai"]), ("Asia/Karachi", LDesigner["TzKarachi"]), ("Asia/Kolkata", LDesigner["TzKolkata"]),
        ("Asia/Dhaka", LDesigner["TzDhaka"]), ("Asia/Bangkok", LDesigner["TzBangkok"]), ("Asia/Singapore", LDesigner["TzSingapore"]),
        ("Asia/Hong_Kong", LDesigner["TzHongKong"]), ("Asia/Shanghai", LDesigner["TzShanghai"]), ("Asia/Taipei", LDesigner["TzTaipei"]),
        ("Asia/Tokyo", LDesigner["TzTokyo"]), ("Asia/Seoul", LDesigner["TzSeoul"]),
        ("Australia/Sydney", LDesigner["TzSydney"]), ("Australia/Melbourne", LDesigner["TzMelbourne"]),
        ("Australia/Perth", LDesigner["TzPerth"]),
        ("Pacific/Auckland", LDesigner["TzAuckland"]),
        ("America/New_York", LDesigner["TzNewYork"]), ("America/Chicago", LDesigner["TzChicago"]),
        ("America/Denver", LDesigner["TzDenver"]), ("America/Los_Angeles", LDesigner["TzLosAngeles"]),
        ("America/Toronto", LDesigner["TzToronto"]), ("America/Vancouver", LDesigner["TzVancouver"]),
        ("America/Mexico_City", LDesigner["TzMexicoCity"]), ("America/Sao_Paulo", LDesigner["TzSaoPaulo"]),
        ("America/Argentina/Buenos_Aires", LDesigner["TzBuenosAires"]), ("America/Bogota", LDesigner["TzBogota"]),
        ("America/Lima", LDesigner["TzLima"]),
        ("Africa/Cairo", LDesigner["TzCairo"]), ("Africa/Lagos", LDesigner["TzLagos"]),
        ("Africa/Johannesburg", LDesigner["TzJohannesburg"]), ("Africa/Nairobi", LDesigner["TzNairobi"]),
    ];

    [Parameter] public Guid WorkflowId { get; set; }

    private BlazorDiagram Diagram { get; set; } = null!;
    private NodeType? _draggedType;
    private Model? SelectedModel { get; set; }

    private Dictionary<Guid, ClientChannelWithClientDto> _recipientDetailsCache = new();
    private bool IsClientPickerOpen { get; set; }
    private IHasRecipient? _activeNodeForPicker;

    private List<AttributeDefinitionDto> CompanyAttributeDefinitions { get; set; } = new();

    private bool IsVariablesModalOpen { get; set; }
    private int VariablesModalTab { get; set; }

    private List<VariableInfo> DiscoveredVariables { get; set; } = new();
    private string VariableSearchQuery { get; set; } = "";

    private string NodeSearchQuery { get; set; } = "";

    private double _zoomLevel = 1.0;
    private int ZoomPercent => (int)Math.Round(_zoomLevel * 100);

    private DateTime? LastSavedAt { get; set; }

    private Func<string?, string> FormatPreviewForNodeWidget => s => ToDisplayText(s ?? "");

    private string LastSavedText
    {
        get
        {
            if (LastSavedAt == null) return LDesigner["NotSaved"];
            var diff = DateTime.UtcNow - LastSavedAt.Value;
            if (diff.TotalMinutes < 1) return LDesigner["SavedJustNow"];
            if (diff.TotalMinutes < 60) return string.Format(LDesigner["SavedMinutesAgo"], (int)diff.TotalMinutes);
            return string.Format(LDesigner["SavedHoursAgo"], (int)diff.TotalHours);
        }
    }

    private readonly List<(string N, string E, string L)> _undoStack = new();
    private int _undoIndex = -1;
    private bool _isRestoring;
    private const int MaxUndoSteps = 30;

    private object? _atMentionTargetObj;
    private string? _atMentionTargetProp;
    private string _atMentionFullValue = "";
    private int _atMentionIndex;

    private bool IsVariablePickerOpen { get; set; }
    private string VariablePickerSearch { get; set; } = "";
    private WorkflowNodeModel? VariablePickerSelectedNode { get; set; }
    private Action<string>? OnVariableSelected { get; set; }

    private bool _jsonMenuOpen;

    private bool _conditionTypeDropdownOpen;
    private int _filterOperatorDropdownOpenIdx = -1;

    private string? _expandedHeaderKey;

    private bool IsStoragePickerOpen { get; set; }
    private MediaNodeData? StoragePickerTarget { get; set; }
    private List<FileInfoDto> StoragePickerAllFiles { get; set; } = [];
    private string StoragePickerSearch { get; set; } = "";
    private int StoragePickerPageIndex { get; set; }
    private const int StoragePickerPageSize = 10;
    private Timer? _storagePickerSearchTimer;
    private bool StoragePickerLoading { get; set; }

    private List<WorkflowListItem>? AvailableWorkflows { get; set; }

    private bool IsProcessPickerOpen { get; set; }
    private SubWorkflowNodeData? ProcessPickerTarget { get; set; }
    private List<WorkflowListItem> ProcessPickerItems { get; set; } = [];
    private string ProcessPickerSearch { get; set; } = "";
    private bool ProcessPickerHasNext { get; set; }
    private bool ProcessPickerHasPrev { get; set; }
    private string? ProcessPickerEndCursor { get; set; }
    private string? ProcessPickerStartCursor { get; set; }
    private bool ProcessPickerLoading { get; set; }

    private List<WorkflowParameterDto> CurrentWorkflowInputParameters { get; set; } = [];
    private List<WorkflowParameterDto> CurrentWorkflowOutputParameters { get; set; } = [];

    private bool IsAiModalOpen { get; set; }

    private readonly Dictionary<Guid, string> _nodeTitleById = new();
    private readonly Dictionary<Guid, string> _nodePrefixById = new();
    private readonly Dictionary<string, List<Guid>> _nodeIdsByTitle = new(StringComparer.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync()
    {
        InitializeDiagram();
        await LoadUserTimezone();
        await LoadWorkflowData();
        await LoadCompanyAttributes();
        await LoadAvailableChannels();
        RefreshVariables();
        RefreshNodeVariableCache();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_conditionTypeDropdownOpen)
            await JSRuntime.InvokeVoidAsync("positionConditionTypeMenu", "condition-type-trigger", "condition-type-menu");
    }

    private async Task LoadCompanyAttributes()
    {
        try
        {
            CompanyAttributeDefinitions = await ClientApiClient.GetCompanyAttributeDefinitionsAsync();
        }
        catch
        {
            CompanyAttributeDefinitions = new List<AttributeDefinitionDto>();
        }
    }

    private async Task LoadAvailableChannels()
    {
        try
        {
            AvailableChannels = await ApiClient.GetChannelsAsync();
        }
        catch
        {
            AvailableChannels = [];
        }
    }

    private async Task LoadUserTimezone()
    {
        try
        {
            UserTimezone = await JSRuntime.InvokeAsync<string>("__getUserTimezone");
        }
        catch
        {
            UserTimezone = "UTC";
        }

        try
        {
            var idsJson = JsonSerializer.Serialize(CommonTimezones.Select(t => t.Id));
            var json = await JSRuntime.InvokeAsync<string>("__getTimezoneOffsets", idsJson);
            TimezoneOffsets = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch
        {
            TimezoneOffsets = CommonTimezones.ToDictionary(t => t.Id, _ => "+0");
        }
    }

    private void AddFilterCondition(TimerStartNodeData timerData)
    {
        timerData.ClientFilter ??= new ClientFilterCriteria();
        timerData.ClientFilter.AttributeConditions.Add(new ClientAttributeFilterCondition());
        OnWorkflowChanged();
    }

    private void RemoveFilterCondition(TimerStartNodeData timerData, ClientAttributeFilterCondition cond)
    {
        timerData.ClientFilter?.AttributeConditions.Remove(cond);
        OnWorkflowChanged();
    }

    private void ToggleFilterIgnoreCase(ClientAttributeFilterCondition cond, ChangeEventArgs e)
    {
        cond.IgnoreCase = e.Value?.ToString() == "true";
        OnWorkflowChanged();
    }

    private string GetFilterLogicLabel(string logic) =>
        logic == "or" ? LDesigner["ConditionOrDescription"] : LDesigner["ConditionAndDescription"];

    private void SetFilterLogicAnd(TimerStartNodeData timerData)
    {
        if (timerData.ClientFilter != null) { timerData.ClientFilter.Logic = "and"; OnWorkflowChanged(); }
    }

    private void SetFilterLogicOr(TimerStartNodeData timerData)
    {
        if (timerData.ClientFilter != null) { timerData.ClientFilter.Logic = "or"; OnWorkflowChanged(); }
    }

    private IReadOnlyList<(string Value, string Label)> FilterOperatorOptions => new List<(string, string)>
    {
        ("equals", LDesigner["EqualsLabel"]),
        ("notequals", LDesigner["NotEqualsLabel"]),
        ("contains", LDesigner["ContainsLabel"]),
        ("startswith", LDesigner["StartsWithLabel"]),
        ("endswith", LDesigner["EndsWithLabel"]),
        ("greaterThan", LDesigner["GreaterThanLabel"]),
        ("lessThan", LDesigner["LessThanLabel"]),
        ("greaterOrEqual", LDesigner["GreaterOrEqualLabel"]),
        ("lessOrEqual", LDesigner["LessOrEqualLabel"]),
        ("inList", LDesigner["InListLabel"]),
        ("regex", LDesigner["RegexLabel"]),
        ("isEmpty", LDesigner["IsEmptyLabel"]),
        ("isNotEmpty", LDesigner["IsNotEmptyLabel"]),
    };

    private string GetFilterOperatorLabel(string op) =>
        FilterOperatorOptions.FirstOrDefault(o => o.Value == op).Label ?? op;

    private static string GetFilterOperatorBadgeClass(string op) => op switch
    {
        "equals" => "designer-cond-badge designer-cond-badge--equals",
        "notequals" => "designer-cond-badge designer-cond-badge--notequals",
        "contains" => "bg-success",
        "startswith" => "bg-success",
        "endswith" => "bg-success",
        "greaterThan" => "bg-warning text-dark",
        "lessThan" => "bg-warning text-dark",
        "greaterOrEqual" => "bg-warning text-dark",
        "lessOrEqual" => "bg-warning text-dark",
        "inList" => "bg-dark",
        "regex" => "bg-dark",
        "isEmpty" => "bg-secondary",
        "isNotEmpty" => "bg-secondary",
        _ => "bg-secondary"
    };

    private string GetFilterOperatorBadgeText(string op) => op switch
    {
        "equals" => LDesigner["BadgeEquals"],
        "notequals" => LDesigner["BadgeNotEquals"],
        "contains" => LDesigner["BadgeContains"],
        "startswith" => LDesigner["BadgeStartsWith"],
        "endswith" => LDesigner["BadgeEndsWith"],
        "greaterThan" => LDesigner["BadgeGreaterThan"],
        "lessThan" => LDesigner["BadgeLessThan"],
        "greaterOrEqual" => LDesigner["BadgeGreaterOrEqual"],
        "lessOrEqual" => LDesigner["BadgeLessOrEqual"],
        "inList" => LDesigner["BadgeInList"],
        "regex" => LDesigner["BadgeRegex"],
        "isEmpty" => LDesigner["BadgeIsEmpty"],
        "isNotEmpty" => LDesigner["BadgeIsNotEmpty"],
        _ => op.ToUpperInvariant()
    };

    private IEnumerable<string> GetAttributeKeysForModal()
    {
        var baseKeys = new[] { "name", "username", "phone", "email" };
        var fromCompany = CompanyAttributeDefinitions.Select(a => a.Key).Where(k => !string.IsNullOrWhiteSpace(k));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in baseKeys)
        {
            if (seen.Add(k)) yield return k;
        }
        foreach (var k in fromCompany)
        {
            if (seen.Add(k)) yield return k;
        }
    }

    private string GetAttributeDisplayName(string key) => key?.ToLowerInvariant() switch
    {
        "name" => LDesigner["VarNameLabel"],
        "username" => "Username",
        "phone" => LDesigner["VarPhoneLabel"],
        "email" => "Email",
        _ => key ?? ""
    };

    private IEnumerable<string> GetAttributeKeysForDropdown()
    {
        var baseKeys = new[] { "name", "username", "phone", "email" };
        var fromCompany = CompanyAttributeDefinitions.Select(a => a.Key).Where(k => !string.IsNullOrWhiteSpace(k));
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var k in baseKeys)
        {
            if (seen.Add(k)) yield return k;
        }
        foreach (var k in fromCompany)
        {
            if (seen.Add(k)) yield return k;
        }
    }

    private string GetAttributeDropdownLabel(string key)
    {
        var attr = CompanyAttributeDefinitions.FirstOrDefault(a =>
            string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase));
        if (attr != null && !string.IsNullOrWhiteSpace(attr.DisplayName))
            return attr.DisplayName;
        return key;
    }

    private static string NormalizeAttributeKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var v = value.Trim();
        if (v.StartsWith("$global.", StringComparison.OrdinalIgnoreCase))
            return v["$global.".Length..];
        return v;
    }

    private void OnAttributeKeyChanged(SetAttributeNodeData attrData, ChangeEventArgs e)
    {
        attrData.Attribute = (e.Value as string) ?? "";
        OnWorkflowChanged();
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
                DefaultSelectedColor = "#6366f1",
                Factory = (diagram, source, targetAnchor) =>
                {
                    Anchor sourceAnchor = source is PortModel port
                        ? new SinglePortAnchor(port)
                        : new ShapeIntersectionAnchor((NodeModel)source);

                    return new WorkflowLinkModel(sourceAnchor, targetAnchor);
                }
            }
        };

        Diagram = new BlazorDiagram(options);
        Diagram.RegisterComponent<WorkflowNodeModel, WorkflowDesignerNodeWidget>();
        Diagram.SelectionChanged += OnSelectionChanged;
        Diagram.Changed += OnDiagramChanged;
    }

    private void OnDiagramChanged()
    {
        if (_isRestoring) return;
        PushUndoState();
        RefreshVariables();
        RefreshNodeVariableCache();
        StateHasChanged();
    }

    private void OnWorkflowChanged()
    {
        RefreshVariables();
        RefreshNodeVariableCache();
        StateHasChanged();
    }

    #region Загрузка и сохранение

    private string? _loadError;

    private async Task LoadWorkflowData()
    {
        _loadError = null;
        var data = await ApiClient.GetWorkflowByIdAsync(WorkflowId);
        if (data == null)
        {
            _loadError = LDesigner["WorkflowNotFound"];
            return;
        }

        try
        {
            var schema = SchemaService.Deserialize(
                data.NodesDefinition,
                data.EdgesDefinition,
                data.LayoutDefinition);
            CurrentWorkflowInputParameters = DeserializeParameters(data.InputParametersDefinition);
            CurrentWorkflowOutputParameters = DeserializeParameters(data.OutputParametersDefinition);
            _isRestoring = true;
            try
            {
                ApplySchemaToDiagram(schema);
            }
            finally
            {
                _isRestoring = false;
            }
            _undoStack.Clear();
            _undoIndex = -1;
            PushUndoState();
        }
        catch (Exception ex)
        {
            _loadError = string.Format(LDesigner["LoadSchemaError"], ex.Message);
        }
    }

    private static List<WorkflowParameterDto> DeserializeParameters(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "[]" || json.Trim() == "{}")
            return [];
        try
        {
            var list = JsonSerializer.Deserialize<List<WorkflowParameterDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return list ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void ApplySchemaToDiagram(WorkflowSchema schema)
    {
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

        foreach (var node in Diagram.Nodes.Cast<WorkflowNodeModel>())
        {
            if (node.Data is AskNodeData ask)
                NormalizeAskButtons(ask);
            if (node.Data is TimerStartNodeData timer && timer.Timezone == "UTC")
                timer.Timezone = UserTimezone;
        }

        RefreshNodeVariableCache();
    }

    private static void NormalizeAskButtons(AskNodeData ask)
    {
        if (ask.Ui?.Buttons == null) return;
        foreach (var b in ask.Ui.Buttons)
        {
            if (string.IsNullOrWhiteSpace(b.Text) && !string.IsNullOrWhiteSpace(b.Value))
                b.Text = b.Value!;
            b.Value = null;
        }
    }

    private void PushUndoState()
    {
        var nodes = Diagram.Nodes.Cast<WorkflowNodeModel>().Select(n => new NodeDefinition(
            Guid.Parse(n.Id), n.NodeType, n.Title ?? "", n.Data is EmptyNodeData ? null : n.Data)).ToList();
        var edges = Diagram.Links.Cast<WorkflowLinkModel>().Select(l =>
        {
            var (fromId, toId) = GetEdgeFromTo(l);
            return (fromId.HasValue && toId.HasValue) ? new EdgeDefinition(fromId.Value, toId.Value, l.Label, l.Condition) : null;
        }).Where(e => e != null).Select(e => e!).ToList();
        var layout = Diagram.Nodes.Select(n => new LayoutDefinition(Guid.Parse(n.Id), n.Position.X, n.Position.Y)).ToList();
        var schema = new WorkflowSchema(nodes, edges, layout);
        var (nStr, eStr, lStr) = SchemaService.Serialize(schema);

        if (_undoIndex < _undoStack.Count - 1)
            _undoStack.RemoveRange(_undoIndex + 1, _undoStack.Count - _undoIndex - 1);
        if (_undoStack.Count >= MaxUndoSteps)
        {
            _undoStack.RemoveAt(0);
            _undoIndex = Math.Max(-1, _undoIndex - 1);
        }
        _undoStack.Add((nStr, eStr, lStr));
        _undoIndex = _undoStack.Count - 1;
    }

    private void Undo()
    {
        if (_undoIndex <= 0) return;
        _undoIndex--;
        RestoreUndoState(_undoStack[_undoIndex]);
    }

    private void Redo()
    {
        if (_undoIndex >= _undoStack.Count - 1) return;
        _undoIndex++;
        RestoreUndoState(_undoStack[_undoIndex]);
    }

    private void RestoreUndoState((string N, string E, string L) state)
    {
        var schema = SchemaService.Deserialize(state.N, state.E, state.L);
        _isRestoring = true;
        try
        {
            ApplySchemaToDiagram(schema);
        }
        finally
        {
            _isRestoring = false;
        }
        RefreshVariables();
        StateHasChanged();
    }

    private async Task SaveWorkflow()
    {
        var nodes = Diagram.Nodes.Cast<WorkflowNodeModel>().Select(n => new NodeDefinition(
            Guid.Parse(n.Id),
            n.NodeType,
            n.Title,
            n.Data is EmptyNodeData ? null : n.Data)).ToList();

        var edges = Diagram.Links.Cast<WorkflowLinkModel>().Select(l =>
        {
            var (fromId, toId) = GetEdgeFromTo(l);
            return (fromId.HasValue && toId.HasValue)
                ? new EdgeDefinition(fromId.Value, toId.Value, l.Label, l.Condition)
                : null;
        }).Where(e => e != null).Select(e => e!).ToList();

        var layout = Diagram.Nodes.Select(n => new LayoutDefinition(
            Guid.Parse(n.Id),
            n.Position.X,
            n.Position.Y)).ToList();

        var schema = new WorkflowSchema(nodes, edges, layout);
        var (nStr, eStr, lStr) = SchemaService.Serialize(schema);

        await ApiClient.UpdateWorkflowDefinitionsAsync(WorkflowId, nStr, eStr, lStr, CurrentWorkflowInputParameters, CurrentWorkflowOutputParameters);
        LastSavedAt = DateTime.UtcNow;
    }

    private static readonly JsonSerializerOptions WorkflowJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private WorkflowSchema GetCurrentSchema()
    {
        var nodes = Diagram.Nodes.Cast<WorkflowNodeModel>().Select(n => new NodeDefinition(
            Guid.Parse(n.Id),
            n.NodeType,
            n.Title ?? "",
            n.Data is EmptyNodeData ? null : n.Data)).ToList();
        var edges = Diagram.Links.Cast<WorkflowLinkModel>().Select(l =>
        {
            var (fromId, toId) = GetEdgeFromTo(l);
            return (fromId.HasValue && toId.HasValue)
                ? new EdgeDefinition(fromId.Value, toId.Value, l.Label, l.Condition)
                : null;
        }).Where(e => e != null).Select(e => e!).ToList();
        var layout = Diagram.Nodes.Select(n => new LayoutDefinition(
            Guid.Parse(n.Id),
            n.Position.X,
            n.Position.Y)).ToList();
        return new WorkflowSchema(nodes, edges, layout, CurrentWorkflowInputParameters, CurrentWorkflowOutputParameters);
    }

    private async Task CloseJsonMenuAndDownload()
    {
        _jsonMenuOpen = false;
        await DownloadWorkflowJson();
    }

    private async Task TriggerImportJsonFile()
    {
        _jsonMenuOpen = false;
        StateHasChanged();
        await JSRuntime.InvokeVoidAsync("window.__triggerWorkflowJsonFileInput");
    }

    private async Task DownloadWorkflowJson()
    {
        var schema = GetCurrentSchema();
        var json = JsonSerializer.Serialize(schema, WorkflowJsonOptions);
        var fileName = $"workflow-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
        await JSRuntime.InvokeVoidAsync("window.__downloadWorkflowJson", fileName, json);
    }

    private async Task OnImportWorkflowJson(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file == null || file.Size == 0) return;
        try
        {
            await using var stream = file.OpenReadStream(maxAllowedSize: 2 * 1024 * 1024);
            var schema = await JsonSerializer.DeserializeAsync<WorkflowSchema>(stream, WorkflowJsonOptions);
            if (schema == null) return;
            if (schema.InputParameters != null)
                CurrentWorkflowInputParameters = schema.InputParameters.Select(p => new WorkflowParameterDto { Name = p.Name ?? "", DefaultValue = p.DefaultValue, Description = p.Description }).ToList();
            if (schema.OutputParameters != null)
                CurrentWorkflowOutputParameters = schema.OutputParameters.Select(p => new WorkflowParameterDto { Name = p.Name ?? "", DefaultValue = p.DefaultValue, Description = p.Description }).ToList();
            _isRestoring = true;
            try
            {
                ApplySchemaToDiagram(schema);
            }
            finally
            {
                _isRestoring = false;
            }
            _undoStack.Clear();
            _undoIndex = -1;
            PushUndoState();
            RefreshVariables();
            _jsonMenuOpen = false;
        }
        catch (Exception ex)
        {
            await JSRuntime.InvokeVoidAsync("alert", string.Format(LDesigner["JsonLoadError"], ex.Message));
        }
    }

    #endregion

    #region Работа с узлами

    private NodeModel CreateNodeInstance(string type, string label, Point position, Guid? id = null,
        NodeData? data = null)
    {
        if (data == null && type.ToLower() == "message")
        {
            data = new MessageNodeData { Text = "" };
        }
        else if (data == null && type.ToLower() == "ask")
        {
            data = new AskNodeData { Text = "" };
        }
        else if (data == null && type.ToLower() == "setattribute")
        {
            data = new SetAttributeNodeData { Attribute = "", Value = "" };
        }
        else if (data == null && type.ToLower() == "media")
        {
            data = new MediaNodeData { SourceType = MediaSourceType.Attachment };
        }
        else if (data == null && type.ToLower() == "subworkflow")
        {
            data = new SubWorkflowNodeData();
        }
        else if (data == null)
        {
            data = new EmptyNodeData();
        }

        var node = new WorkflowNodeModel(position, type, label, id, data);

        switch (type.ToLower())
        {
            case "start":
            case "timerstart":
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

    private void OnDrop(DragEventArgs e)
    {
        if (_draggedType == null) return;
        var point = Diagram.GetRelativeMousePoint(e.ClientX, e.ClientY);

        var label = _draggedType.Value switch
        {
            NodeType.Start => LDesigner["NodeLabelStart"],
            NodeType.End => LDesigner["NodeLabelEnd"],
            NodeType.Message => LDesigner["NodeLabelMessage"],
            NodeType.Ask => LDesigner["NodeLabelAsk"],
            NodeType.Wait => LDesigner["NodeLabelWait"],
            NodeType.WebhookWait => LDesigner["NodeLabelWebhookWait"],
            NodeType.SetAttribute => LDesigner["NodeLabelSetAttribute"],
            NodeType.HttpRequest => LDesigner["NodeLabelHttpRequest"],
            NodeType.AIGenerate => LDesigner["NodeLabelAIGenerate"],
            NodeType.Media => LDesigner["NodeLabelMedia"],
            NodeType.SubWorkflow => LDesigner["NodeLabelSubWorkflow"],
            _ => LDesigner["NodeLabelDefault"]
        };

        NodeData? initialData = _draggedType.Value switch
        {
            NodeType.Message => new MessageNodeData { Text = "" },
            NodeType.Ask => new AskNodeData { Text = "" },
            NodeType.SetAttribute => new SetAttributeNodeData { Attribute = "", Value = "" },
            NodeType.HttpRequest => new HttpRequestNodeData { Method = "GET", Headers = new(), Url = "" },
            NodeType.AIGenerate => new AIGenerateNodeData { Prompt = "" },
            NodeType.Media => new MediaNodeData { SourceType = MediaSourceType.Attachment },
            NodeType.SubWorkflow => new SubWorkflowNodeData(),
            NodeType.Wait => new WaitNodeData(),
            NodeType.WebhookWait => new WebhookWaitNodeData(),
            NodeType.TimerStart => new TimerStartNodeData { Timezone = UserTimezone },
            _ => null
        };

        var node = CreateNodeInstance(_draggedType.Value.ToString(), label, point, data: initialData);
        Diagram.Nodes.Add(node);
        _draggedType = null;

        RefreshVariables();
    }

    private void DeleteSelectedModel()
    {
        if (SelectedModel == null) return;

        if (SelectedModel is NodeModel node)
            Diagram.Nodes.Remove(node);
        else if (SelectedModel is LinkModel link)
            Diagram.Links.Remove(link);

        SelectedModel = null;
        RefreshVariables();
    }

    private void AddHeader(HttpRequestNodeData httpData)
    {
        if (httpData?.Headers != null)
        {
            httpData.Headers.Add($"Header-{httpData.Headers.Count + 1}", "");
            OnWorkflowChanged();
        }
    }

    private void RemoveHeader(HttpRequestNodeData httpData, string key)
    {
        if (httpData?.Headers != null)
        {
            httpData.Headers.Remove(key);
            OnWorkflowChanged();
        }
    }

    private void AddAskButton(AskNodeData askData)
    {
        if (askData == null) return;
        askData.Ui ??= new AskUiData();
        askData.Ui.Buttons.Add(new AskButtonData { Text = "" });
        OnWorkflowChanged();
        StateHasChanged();
    }

    private void RemoveAskButton(AskNodeData askData, AskButtonData button)
    {
        if (askData?.Ui?.Buttons == null) return;
        askData.Ui.Buttons.Remove(button);
        OnWorkflowChanged();
        StateHasChanged();
    }

    private async Task LoadAvailableWorkflows()
    {
        try
        {
            AvailableWorkflows = (await ApiClient.GetWorkflowsListAsync())
                .Where(w => w.Id != WorkflowId)
                .ToList();
        }
        catch
        {
            AvailableWorkflows = [];
        }

        StateHasChanged();
    }

    private void OpenProcessPicker(SubWorkflowNodeData subData)
    {
        ProcessPickerTarget = subData;
        ProcessPickerSearch = "";
        ProcessPickerItems = [];
        ProcessPickerEndCursor = null;
        ProcessPickerStartCursor = null;
        IsProcessPickerOpen = true;
        _ = LoadProcessPickerPage();
    }

    private void CloseProcessPicker()
    {
        IsProcessPickerOpen = false;
        ProcessPickerTarget = null;
        ProcessPickerItems = [];
        StateHasChanged();
    }

    private async Task LoadProcessPickerPage(string? after = null, string? before = null)
    {
        if (!IsProcessPickerOpen) return;
        ProcessPickerLoading = true;
        StateHasChanged();
        try
        {
            WorkflowListPage page;
            if (before != null)
                page = await ApiClient.GetWorkflowsPageAsync(last: 10, before: before);
            else
                page = await ApiClient.GetWorkflowsPageAsync(first: 10, after: after);
            var items = page.Items.Where(w => w.Id != WorkflowId).ToList();
            if (!string.IsNullOrWhiteSpace(ProcessPickerSearch))
            {
                var q = ProcessPickerSearch.Trim();
                items = items.Where(w => GetWorkflowDisplayName(w).Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            ProcessPickerItems = items;
            ProcessPickerHasNext = page.HasNextPage;
            ProcessPickerHasPrev = page.HasPreviousPage;
            ProcessPickerEndCursor = page.EndCursor;
            ProcessPickerStartCursor = page.StartCursor;
        }
        catch
        {
            ProcessPickerItems = [];
            ProcessPickerHasNext = false;
            ProcessPickerHasPrev = false;
        }
        finally
        {
            ProcessPickerLoading = false;
            StateHasChanged();
        }
    }

    private async Task ProcessPickerNextPage()
    {
        if (string.IsNullOrEmpty(ProcessPickerEndCursor)) return;
        await LoadProcessPickerPage(after: ProcessPickerEndCursor);
    }

    private async Task ProcessPickerPrevPage()
    {
        if (string.IsNullOrEmpty(ProcessPickerStartCursor)) return;
        await LoadProcessPickerPage(before: ProcessPickerStartCursor);
    }

    private void OnProcessPickerSearchSubmit() => _ = LoadProcessPickerPage();

    private void SelectProcessFromPicker(WorkflowListItem w)
    {
        if (ProcessPickerTarget == null) return;
        ProcessPickerTarget.WorkflowId = w.Id;
        ProcessPickerTarget.InputMappings = w.InputParameters.ToDictionary(p => p.Name, p => p.DefaultValue ?? "");
        var outDict = new Dictionary<string, string>();
        foreach (var p in w.OutputParameters)
            outDict[$"result_{outDict.Count + 1}"] = p.Name;
        ProcessPickerTarget.OutputMappings = outDict;
        AvailableWorkflows ??= [];
        if (AvailableWorkflows.All(x => x.Id != w.Id))
            AvailableWorkflows.Insert(0, w);
        OnWorkflowChanged();
        CloseProcessPicker();
    }

    private string GetWorkflowDisplayName(WorkflowListItem w) =>
        w.Bot != null ? $"{w.Bot.Name} (v{w.Version})" : $"Workflow v{w.Version}";

    private async Task OnSubWorkflowSelected(SubWorkflowNodeData subData, ChangeEventArgs e)
    {
        var idStr = e.Value?.ToString();
        if (Guid.TryParse(idStr, out var id))
        {
            subData.WorkflowId = id;
            var target = AvailableWorkflows?.FirstOrDefault(w => w.Id == id);
            if (target != null)
            {
                subData.InputMappings = target.InputParameters
                    .ToDictionary(p => p.Name, p => p.DefaultValue ?? "");
                var outDict = new Dictionary<string, string>();
                foreach (var p in target.OutputParameters)
                    outDict[$"result_{outDict.Count + 1}"] = p.Name;
                subData.OutputMappings = outDict;
            }
        }
        else
        {
            subData.WorkflowId = Guid.Empty;
            subData.InputMappings.Clear();
            subData.OutputMappings.Clear();
        }

        OnWorkflowChanged();
    }

    private void InsertVariableIntoMapping(Dictionary<string, string> mappings, string key, string variableName)
    {
        var toInsert = "{{" + VariableNameToStorageForm(variableName) + "}}";
        mappings[key] = (mappings.GetValueOrDefault(key) ?? "") + toInsert;
        OnWorkflowChanged();
        StateHasChanged();
    }

    private void AddSubWorkflowInputMapping(SubWorkflowNodeData subData)
    {
        var key = $"param_{subData.InputMappings.Count + 1}";
        subData.InputMappings[key] = "";
        OnWorkflowChanged();
    }

    private void RemoveSubWorkflowInputMapping(SubWorkflowNodeData subData, string key)
    {
        subData.InputMappings.Remove(key);
        OnWorkflowChanged();
    }

    private void AddSubWorkflowOutputMapping(SubWorkflowNodeData subData)
    {
        var key = $"result_{subData.OutputMappings.Count + 1}";
        subData.OutputMappings[key] = "";
        OnWorkflowChanged();
    }

    private void RemoveSubWorkflowOutputMapping(SubWorkflowNodeData subData, string key)
    {
        subData.OutputMappings.Remove(key);
        OnWorkflowChanged();
    }

    private void AddWorkflowInputParameter()
    {
        CurrentWorkflowInputParameters.Add(new WorkflowParameterDto { Name = "", DefaultValue = "", Description = "" });
        OnWorkflowChanged();
        StateHasChanged();
    }

    private void RemoveWorkflowInputParameter(WorkflowParameterDto item)
    {
        CurrentWorkflowInputParameters.Remove(item);
        OnWorkflowChanged();
        StateHasChanged();
    }

    private void AddWorkflowOutputParameter()
    {
        CurrentWorkflowOutputParameters.Add(new WorkflowParameterDto { Name = "", Description = "" });
        OnWorkflowChanged();
        StateHasChanged();
    }

    private void RemoveWorkflowOutputParameter(WorkflowParameterDto item)
    {
        CurrentWorkflowOutputParameters.Remove(item);
        OnWorkflowChanged();
        StateHasChanged();
    }

    private async Task OpenStoragePicker(MediaNodeData mediaData)
    {
        StoragePickerTarget = mediaData;
        IsStoragePickerOpen = true;
        StoragePickerLoading = true;
        StoragePickerAllFiles = [];
        StoragePickerSearch = "";
        StoragePickerPageIndex = 0;
        StateHasChanged();
        try
        {
            StoragePickerAllFiles = await FileApiClient.ListFilesAsync(workflowId: WorkflowId);
        }
        catch
        {
            StoragePickerAllFiles = [];
        }
        finally
        {
            StoragePickerLoading = false;
            StateHasChanged();
        }
    }

    private void CloseStoragePicker()
    {
        _storagePickerSearchTimer?.Dispose();
        _storagePickerSearchTimer = null;
        IsStoragePickerOpen = false;
        StoragePickerTarget = null;
        StoragePickerAllFiles = [];
        StoragePickerSearch = "";
        StoragePickerPageIndex = 0;
        StateHasChanged();
    }

    private List<FileInfoDto> ComputeStoragePickerFiltered()
    {
        IEnumerable<FileInfoDto> q = StoragePickerAllFiles;
        var search = StoragePickerSearch.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            q = q.Where(f =>
                (f.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) == true) ||
                f.Id.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        return q.OrderByDescending(f => f.CreatedAt).ToList();
    }

    private (List<FileInfoDto> page, int total) GetStoragePickerPage()
    {
        var filtered = ComputeStoragePickerFiltered();
        var total = filtered.Count;
        if (total == 0)
            return (new List<FileInfoDto>(), 0);
        var maxPageIndex = (total - 1) / StoragePickerPageSize;
        if (StoragePickerPageIndex > maxPageIndex)
            StoragePickerPageIndex = maxPageIndex;
        var page = filtered.Skip(StoragePickerPageIndex * StoragePickerPageSize).Take(StoragePickerPageSize).ToList();
        return (page, total);
    }

    private bool StoragePickerHasPrev => StoragePickerPageIndex > 0;

    private bool StoragePickerHasNext
    {
        get
        {
            var total = ComputeStoragePickerFiltered().Count;
            return (StoragePickerPageIndex + 1) * StoragePickerPageSize < total;
        }
    }

    private void OnStoragePickerSearchInput()
    {
        _storagePickerSearchTimer?.Dispose();
        _storagePickerSearchTimer = new Timer(_ =>
        {
            InvokeAsync(async () =>
            {
                _storagePickerSearchTimer?.Dispose();
                _storagePickerSearchTimer = null;
                StoragePickerPageIndex = 0;
                await InvokeAsync(StateHasChanged);
            });
        }, null, 400, Timeout.Infinite);
    }

    private void OnStoragePickerSearchSubmit()
    {
        StoragePickerPageIndex = 0;
        StateHasChanged();
    }

    private void StoragePickerNextPage()
    {
        if (StoragePickerHasNext)
            StoragePickerPageIndex++;
        StateHasChanged();
    }

    private void StoragePickerPrevPage()
    {
        if (StoragePickerHasPrev)
            StoragePickerPageIndex--;
        StateHasChanged();
    }

    private void SelectStorageFile(FileInfoDto file)
    {
        if (StoragePickerTarget != null)
        {
            StoragePickerTarget.Value = file.Id;
            OnWorkflowChanged();
        }
        CloseStoragePicker();
    }

    private async Task OnMediaFileSelected(InputFileChangeEventArgs e, MediaNodeData mediaData)
    {
        var file = e.File;
        if (file == null) return;

        try
        {
            await using var stream = file.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024);
            var contentType = file.ContentType;
            var result = await FileApiClient.UploadFileAsync(stream, file.Name, contentType, workflowId: WorkflowId);
            if (result != null)
            {
                mediaData.Value = result.Id;
                mediaData.MediaKind = DetectMediaKindFromFileName(file.Name);
                OnWorkflowChanged();
                StateHasChanged();
            }
        }
        catch (Exception ex)
        {
            // TODO: показать пользователю ошибку (например через toast или лог)
            Console.WriteLine(ex.Message);
        }
    }

    private static MediaKind DetectMediaKindFromFileName(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp" => MediaKind.Image,
            ".mp4" or ".mov" or ".avi" or ".webm" or ".mkv" => MediaKind.Video,
            ".mp3" or ".wav" or ".ogg" or ".m4a" or ".aac" => MediaKind.Audio,
            _ => MediaKind.File
        };
    }

    #endregion

    #region Работа с переменными - Обнаружение

    private List<(string Name, string Description)> GlobalAttributeVariables =>
    [
        ("$global.name", LDesigner["VarNameLabel"]),
        ("$global.username", "Username"),
        ("$global.phone", LDesigner["VarPhoneLabel"]),
        ("$global.email", "Email")
    ];

    private void RefreshVariables()
    {
        var variables = new Dictionary<string, VariableInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var (name, description) in GlobalAttributeVariables)
        {
            variables[name] = new VariableInfo
            {
                Name = name,
                Type = VariableType.GlobalAttribute,
                SourceNode = description
            };
        }
        foreach (var attr in CompanyAttributeDefinitions)
        {
            var name = "$global." + attr.Key;
            if (!variables.ContainsKey(name))
            {
                variables[name] = new VariableInfo
                {
                    Name = name,
                    Type = VariableType.GlobalAttribute,
                    SourceNode = attr.DisplayName ?? attr.Key
                };
            }
        }

        foreach (var p in CurrentWorkflowInputParameters.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
        {
            var name = NormalizeVariableName(p.Name);
            if (!variables.ContainsKey(name))
            {
                variables[name] = new VariableInfo
                {
                    Name = name,
                    Type = VariableType.Custom,
                    SourceNode = string.IsNullOrWhiteSpace(p.Description) ? LDesigner["DefaultParamSource"] : p.Description
                };
            }
        }

        foreach (var node in Diagram.Nodes.Cast<WorkflowNodeModel>())
        {
            var nodeTitle = node.Title ?? "Unnamed";
            foreach (var (key, display) in GetAutoVariablesForNode(node))
            {
                if (!variables.ContainsKey(key))
                {
                    variables[key] = new VariableInfo
                    {
                        Name = key,
                        Type = VariableType.Custom,
                        SourceNode = $"{nodeTitle} · {display}"
                    };
                }
            }
        }

        foreach (var node in Diagram.Nodes.Cast<WorkflowNodeModel>())
        {
            var nodeTitle = node.Title ?? "Unnamed";

            if (node.Data is MessageNodeData msgData && !string.IsNullOrWhiteSpace(msgData.Text))
                TrackUsage(variables, ExtractVariables(msgData.Text), nodeTitle);

            if (node.Data is AskNodeData askData)
            {
                if (!string.IsNullOrWhiteSpace(askData.Text))
                    TrackUsage(variables, ExtractVariables(askData.Text), nodeTitle);

                if (askData.Ui?.Buttons != null)
                {
                    foreach (var b in askData.Ui.Buttons)
                    {
                        if (!string.IsNullOrWhiteSpace(b.Text))
                            TrackUsage(variables, ExtractVariables(b.Text), nodeTitle);
                    }
                }
            }

            if (node.Data is SetAttributeNodeData attrData && !string.IsNullOrWhiteSpace(attrData.Value))
                TrackUsage(variables, ExtractVariables(attrData.Value), nodeTitle);

            if (node.Data is HttpRequestNodeData httpData)
            {
                if (!string.IsNullOrWhiteSpace(httpData.Url))
                    TrackUsage(variables, ExtractVariables(httpData.Url), nodeTitle);
                if (!string.IsNullOrWhiteSpace(httpData.Body))
                    TrackUsage(variables, ExtractVariables(httpData.Body), nodeTitle);
                foreach (var header in httpData.Headers)
                {
                    if (!string.IsNullOrWhiteSpace(header.Value))
                        TrackUsage(variables, ExtractVariables(header.Value), nodeTitle);
                }
            }

            if (node.Data is AIGenerateNodeData aiData && !string.IsNullOrWhiteSpace(aiData.Prompt))
                TrackUsage(variables, ExtractVariables(aiData.Prompt), nodeTitle);

            if (node.Data is MediaNodeData mediaData)
            {
                if (!string.IsNullOrWhiteSpace(mediaData.Value))
                    TrackUsage(variables, ExtractVariables(mediaData.Value), nodeTitle);
                if (!string.IsNullOrWhiteSpace(mediaData.Caption))
                    TrackUsage(variables, ExtractVariables(mediaData.Caption), nodeTitle);
            }

            if (node.Data is SubWorkflowNodeData subData)
            {
                foreach (var mapping in subData.InputMappings)
                {
                    if (!string.IsNullOrWhiteSpace(mapping.Value))
                        TrackUsage(variables, ExtractVariables(mapping.Value), nodeTitle);
                }
            }
        }

        foreach (var link in Diagram.Links.Cast<WorkflowLinkModel>())
        {
            string usageNode = LDesigner["LinkConditionUsage"];

            TrackConditionVariables(link.Condition, variables, usageNode);
        }

        DiscoveredVariables = variables.Values
            .OrderBy(v => v.Type)
            .ThenBy(v => v.Name)
            .ToList();
    }

    private void TrackConditionVariables(ConditionDefinition? cond, Dictionary<string, VariableInfo> variables, string usageNode)
    {
        if (cond == null) return;

        if (cond.And != null)
        {
            foreach (var sub in cond.And)
                TrackConditionVariables(sub, variables, usageNode);
            return;
        }
        if (cond.Or != null)
        {
            foreach (var sub in cond.Or)
                TrackConditionVariables(sub, variables, usageNode);
            return;
        }

        void TrackBinary(BinaryConditionBase? c)
        {
            if (c == null) return;
            var leftVar = NormalizeVariableName(c.Left);
            EnsureVariableExists(variables, leftVar);
            variables[leftVar].UsageNodes.Add(usageNode);
            TrackUsage(variables, ExtractVariables(c.Right), usageNode);
        }

        void TrackUnary(UnaryConditionBase? c)
        {
            if (c == null) return;
            var leftVar = NormalizeVariableName(c.Left);
            EnsureVariableExists(variables, leftVar);
            variables[leftVar].UsageNodes.Add(usageNode);
        }

        TrackBinary(cond.Equals);
        TrackBinary(cond.NotEquals);
        TrackBinary(cond.Contains);
        TrackBinary(cond.StartsWith);
        TrackBinary(cond.EndsWith);
        TrackBinary(cond.GreaterThan);
        TrackBinary(cond.LessThan);
        TrackBinary(cond.GreaterOrEqual);
        TrackBinary(cond.LessOrEqual);
        TrackBinary(cond.InList);
        TrackBinary(cond.Regex);
        TrackUnary(cond.IsEmpty);
        TrackUnary(cond.IsNotEmpty);
    }

    private static void TrackUsage(Dictionary<string, VariableInfo> variables, IEnumerable<string> usedVars, string usageNode)
    {
        foreach (var usedVar in usedVars)
        {
            EnsureVariableExists(variables, usedVar);
            if (!variables[usedVar].UsageNodes.Contains(usageNode))
                variables[usedVar].UsageNodes.Add(usageNode);
        }
    }

    private IReadOnlyList<(string Key, string Display)> GetAutoVariablesForNode(WorkflowNodeModel node)
    {
        if (!Guid.TryParse(node.Id, out var id))
            return [];

        return node.NodeType?.ToLowerInvariant() switch
        {
            "start" =>
            [
                ($"$node.{id}.output", LDesigner["PayloadStart"]),
                ($"$node.{id}.messageKind", LDesigner["MessageKind"])
            ],
            "ask" =>
            [
                ($"$node.{id}.output", LDesigner["UserResponse"]),
                ($"$node.{id}.messageKind", LDesigner["MessageKind"])
            ],
            "aigenerate" => [($"$node.{id}.output", LDesigner["AiResult"]),
                              ($"$node.{id}.statusCode", LDesigner["StatusCode"]),
                              ($"$node.{id}.success", LDesigner["RequestSuccess"])],
            "httprequest" => [($"$node.{id}.output", LDesigner["ResponseBody"]),
                              ($"$node.{id}.statusCode", LDesigner["StatusCode"]),
                              ($"$node.{id}.success", LDesigner["RequestSuccess"])],
            "webhookwait" =>
            [
                ($"$node.{id}.callbackUrl", LDesigner["CallbackUrl"]),
                ($"$node.{id}.output", LDesigner["WebhookResponseBody"])
            ],
            _ => []
        };
    }

    private static string GetVariableKeyShortDisplay(string fullKey)
    {
        if (string.IsNullOrEmpty(fullKey))
            return fullKey ?? "";
        var lastDot = fullKey.LastIndexOf('.');
        return lastDot >= 0 ? fullKey[(lastDot + 1)..] : fullKey;
    }

    private void RefreshNodeVariableCache()
    {
        _nodeTitleById.Clear();
        _nodePrefixById.Clear();
        _nodeIdsByTitle.Clear();

        if (Diagram == null)
            return;

        var ids = new List<(Guid Id, string N)>();
        foreach (var node in Diagram.Nodes.Cast<WorkflowNodeModel>())
        {
            if (!Guid.TryParse(node.Id, out var id))
                continue;

            var title = node.Title ?? node.NodeType ?? LDesigner["NodeLabelDefault"];
            _nodeTitleById[id] = title;
            var normalizedTitle = SanitizeNodeTitleForToken(title);
            if (!_nodeIdsByTitle.TryGetValue(normalizedTitle, out var list))
            {
                list = [];
                _nodeIdsByTitle[normalizedTitle] = list;
            }
            list.Add(id);
            ids.Add((id, id.ToString("N").ToLowerInvariant()));
        }

        if (ids.Count == 0)
            return;

        var assigned = new Dictionary<Guid, string>();
        for (var len = 8; len <= 32; len++)
        {
            var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var (_, n) in ids)
            {
                var p = n[..len];
                counts[p] = counts.TryGetValue(p, out var c) ? c + 1 : 1;
            }

            foreach (var (id, n) in ids)
            {
                if (assigned.ContainsKey(id)) continue;
                var p = n[..len];
                if (counts[p] == 1)
                    assigned[id] = p;
            }

            if (assigned.Count == ids.Count)
                break;
        }

        foreach (var (id, n) in ids)
            _nodePrefixById[id] = assigned.TryGetValue(id, out var p) ? p : n[..8];
    }

    private static readonly Regex NodeInternalVarRegex =
        new(@"\{\{(?:\$?node\.)?(?<guid>[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12})\.(?<key>[a-zA-Z0-9_]+)\}\}",
            RegexOptions.Compiled);

    private static readonly Regex NodeDisplayVarRegexPretty =
        new(@"\{\{(?<label>[^·]+)·(?<key>[a-zA-Z0-9_]+)\}\}", RegexOptions.Compiled);

    private static readonly Regex NodeDisplayVarRegexLegacy =
        new(@"\{\{(?<label>[^#\{\}]+)#(?<prefix>[0-9a-fA-F]{8,32})\.(?<key>[a-zA-Z0-9_]+)\}\}",
            RegexOptions.Compiled);

    private string ToDisplayText(string? internalText)
    {
        if (string.IsNullOrEmpty(internalText))
            return internalText ?? "";

        return NodeInternalVarRegex.Replace(internalText, m =>
        {
            if (!Guid.TryParse(m.Groups["guid"].Value, out var id))
                return m.Value;

            var key = m.Groups["key"].Value;
            var title = _nodeTitleById.TryGetValue(id, out var t) ? t : id.ToString()[..8];
            title = SanitizeNodeTitleForToken(title);

            return "{{" + title + " · " + key + "}}";
        });
    }

    private static bool GetCheckboxChecked(ChangeEventArgs e)
    {
        if (e.Value is bool b) return b;
        var s = e.Value?.ToString();
        return string.Equals(s, "on", StringComparison.OrdinalIgnoreCase) || string.Equals(s, "true", StringComparison.OrdinalIgnoreCase);
    }

    private string FromDisplayText(string? displayText)
    {
        if (string.IsNullOrEmpty(displayText))
            return displayText ?? "";

        var step1 = NodeDisplayVarRegexPretty.Replace(displayText, m =>
        {
            var label = m.Groups["label"].Value.Trim();
            var key = m.Groups["key"].Value;
            var id = ResolveGuidByTitle(label, key);
            if (id == null)
                return m.Value;
            return "{{" + id.Value.ToString("D") + "." + key + "}}";
        });

        return NodeDisplayVarRegexLegacy.Replace(step1, m =>
        {
            var prefix = m.Groups["prefix"].Value.ToLowerInvariant();
            var key = m.Groups["key"].Value;
            var id = ResolveGuidByPrefix(prefix);
            if (id == null)
                return m.Value;
            return "{{" + id.Value.ToString("D") + "." + key + "}}";
        });
    }

    private Guid? ResolveGuidByTitle(string label, string key)
    {
        if (string.IsNullOrWhiteSpace(label))
            return null;

        var normalized = SanitizeNodeTitleForToken(label);
        if (!_nodeIdsByTitle.TryGetValue(normalized, out var ids) || ids.Count == 0)
            return null;

        if (ids.Count == 1)
            return ids[0];

        foreach (var id in ids)
        {
            var node = Diagram?.Nodes.Cast<WorkflowNodeModel>().FirstOrDefault(n => n.Id == id.ToString());
            if (node != null && GetAutoVariablesForNode(node).Any(v => v.Key.EndsWith("." + key, StringComparison.Ordinal)))
                return id;
        }
        return ids[0];
    }

    private Guid? ResolveGuidByPrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
            return null;

        foreach (var (id, p) in _nodePrefixById)
        {
            if (string.Equals(p, prefix, StringComparison.OrdinalIgnoreCase))
                return id;
        }

        foreach (var id in _nodeTitleById.Keys)
        {
            var n = id.ToString("N");
            if (n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return id;
        }

        return null;
    }

    private string SanitizeNodeTitleForToken(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return LDesigner["NodeLabelDefault"];

        var t = title.Trim();
        t = t.Replace("{", " ").Replace("}", " ").Replace("#", " ").Replace("\r", " ").Replace("\n", " ");
        t = Regex.Replace(t, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(t) ? LDesigner["NodeLabelDefault"] : t;
    }

    private static void EnsureVariableExists(Dictionary<string, VariableInfo> variables, string varName)
    {
        if (!variables.ContainsKey(varName))
        {
            variables[varName] = new VariableInfo
            {
                Name = varName,
                Type = GetVariableType(varName),
                SourceNode = null
            };
        }
    }

    private List<string> ExtractVariables(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var variables = new List<string>();
        var pattern = @"\$?\{\{([^}]+)\}\}";
        var matches = Regex.Matches(text, pattern);

        foreach (Match match in matches)
        {
            var varName = match.Groups[1].Value.Trim();
            varName = NormalizeVariableName(varName);
            if (!variables.Contains(varName))
            {
                variables.Add(varName);
            }
        }

        return variables;
    }

    private string NormalizeVariableName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "";

        var normalized = input.Trim();
        normalized = Regex.Replace(normalized, @"^\{\{|\}\}$", "");

        if (!normalized.StartsWith("$"))
        {
            normalized = "$" + normalized;
        }

        return normalized;
    }

    private static VariableType GetVariableType(string varName)
    {
        var lower = varName.ToLower();

        if (lower.StartsWith("$global."))
            return VariableType.GlobalAttribute;

        if (lower.StartsWith("$system.") || lower.StartsWith("$bot."))
            return VariableType.System;

        if (lower.StartsWith("$user."))
            return VariableType.User;

        return VariableType.Custom;
    }

    private string GetVariableCssClass(VariableInfo variable)
    {
        return variable.Type switch
        {
            VariableType.GlobalAttribute => "var-global",
            VariableType.System => "var-system",
            VariableType.User => "var-user",
            VariableType.Custom => "var-custom",
            _ => ""
        };
    }

    private Dictionary<string, List<VariableInfo>> GetGroupedVariables()
    {
        var filtered = string.IsNullOrWhiteSpace(VariableSearchQuery)
            ? DiscoveredVariables
            : DiscoveredVariables.Where(v => v.Name.Contains(VariableSearchQuery, StringComparison.OrdinalIgnoreCase))
                .ToList();

        return filtered.GroupBy(v => v.Type switch
        {
            VariableType.GlobalAttribute => LDesigner["VarTypeAttributes"].Value,
            VariableType.System => LDesigner["VarTypeSystem"].Value,
            VariableType.User => LDesigner["VarTypeUser"].Value,
            VariableType.Custom => LDesigner["VarTypeCustom"].Value,
            _ => LDesigner["VarTypeOther"].Value
        }).ToDictionary(g => g.Key, g => g.ToList());
    }

    private Dictionary<string, List<VariableInfo>> GetGroupedVariablesOnlyVariables()
    {
        var onlyVars = DiscoveredVariables.Where(v => v.Type != VariableType.GlobalAttribute).ToList();
        var filtered = string.IsNullOrWhiteSpace(VariableSearchQuery)
            ? onlyVars
            : onlyVars.Where(v => v.Name.Contains(VariableSearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

        return filtered.GroupBy(v => v.Type switch
        {
            VariableType.System => LDesigner["VarTypeSystem"].Value,
            VariableType.User => LDesigner["VarTypeUser"].Value,
            VariableType.Custom => LDesigner["VarTypeCustom"].Value,
            _ => LDesigner["VarTypeOther"].Value
        }).ToDictionary(g => g.Key, g => g.ToList());
    }

    #endregion

    #region Работа с переменными - UI Actions

    private void OpenVariablesModal()
    {
        IsVariablesModalOpen = true;
        VariablesModalTab = 0;
        RefreshVariables();
    }

    private void CloseVariablesModal()
    {
        IsVariablesModalOpen = false;
    }

    private void ZoomIn()
    {
        _zoomLevel = Math.Min(2.0, _zoomLevel + 0.1);
        Diagram.SetZoom(_zoomLevel);
        StateHasChanged();
    }

    private void ZoomOut()
    {
        _zoomLevel = Math.Max(0.25, _zoomLevel - 0.1);
        Diagram.SetZoom(_zoomLevel);
        StateHasChanged();
    }

    private async Task CopyVariableToClipboard(string variableName)
    {
        var toCopy = "{{" + variableName.TrimStart('$') + "}}";
        await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", toCopy);
    }

    private void ShowVariablePicker(Action<string> onSelected)
    {
        OnVariableSelected = onSelected;
        VariablePickerSearch = "";
        VariablePickerSelectedNode = null;
        IsVariablePickerOpen = true;
    }

    private void CloseVariablePicker()
    {
        IsVariablePickerOpen = false;
        OnVariableSelected = null;
        VariablePickerSelectedNode = null;
    }

    private void SelectVariableFromPicker(string variableName)
    {
        if (_atMentionTargetObj != null && !string.IsNullOrEmpty(_atMentionTargetProp))
        {
            var insert = "{{" + VariableNameToStorageForm(variableName) + "}}";
            var newVal = _atMentionFullValue.Substring(0, _atMentionIndex) + insert + _atMentionFullValue.Substring(_atMentionIndex + 1);
            newVal = FromDisplayText(newVal);
            var prop = _atMentionTargetObj.GetType().GetProperty(_atMentionTargetProp);
            prop?.SetValue(_atMentionTargetObj, newVal);
            _atMentionTargetObj = null;
            _atMentionTargetProp = null;
            OnWorkflowChanged();
        }
        else
        {
            OnVariableSelected?.Invoke(variableName);
        }
        CloseVariablePicker();
        StateHasChanged();
    }

    private void UpdateFieldAndCheckAtMention(ChangeEventArgs e, object dataObject, string propertyName)
    {
        var displayValue = e.Value?.ToString() ?? "";
        var value = FromDisplayText(displayValue);
        var prop = dataObject.GetType().GetProperty(propertyName);
        prop?.SetValue(dataObject, value);
        if (displayValue.Contains('@'))
        {
            _atMentionFullValue = displayValue;
            _atMentionIndex = displayValue.LastIndexOf('@');
            _atMentionTargetObj = dataObject;
            _atMentionTargetProp = propertyName;
            ShowVariablePicker(_ => { });
        }
    }

    private IReadOnlyList<WorkflowNodeModel> GetFilteredNodesForVariablePicker()
    {
        if (Diagram == null)
            return [];

        var nodes = Diagram.Nodes.Cast<WorkflowNodeModel>()
            .Where(n => GetAutoVariablesForNode(n).Count > 0)
            .ToList();
        if (string.IsNullOrWhiteSpace(VariablePickerSearch))
            return nodes.OrderBy(n => n.Title).ToList();

        var q = VariablePickerSearch.Trim();
        return nodes
            .Where(n =>
                (n.Title ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (n.NodeType ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(n => n.Title)
            .ToList();
    }

    private void SelectNodeForVariablePicker(WorkflowNodeModel node)
    {
        VariablePickerSelectedNode = node;
        VariablePickerSearch = "";
    }

    private void BackToVariablePickerNodes()
    {
        VariablePickerSelectedNode = null;
        VariablePickerSearch = "";
    }

    private IReadOnlyList<(string Key, string Display)> GetVariablesForSelectedNode()
    {
        if (VariablePickerSelectedNode == null)
            return [];

        var vars = GetAutoVariablesForNode(VariablePickerSelectedNode);
        if (string.IsNullOrWhiteSpace(VariablePickerSearch))
            return vars;

        var q = VariablePickerSearch.Trim();
        return vars
            .Where(v =>
                v.Key.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                v.Display.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static bool IsNodeOutputVariable(string varName)
    {
        if (string.IsNullOrEmpty(varName))
            return false;
        if (varName.StartsWith("$node.", StringComparison.OrdinalIgnoreCase))
            return true;
        if (varName.StartsWith("$") && varName.Length > 40)
        {
            var afterDollar = varName.AsSpan(1);
            var dot = afterDollar.IndexOf('.');
            if (dot > 0 && Guid.TryParse(afterDollar[..dot].ToString(), out _))
                return true;
        }
        return false;
    }

    private Dictionary<string, List<VariableInfo>> GetGroupedStaticVariablesForPicker()
    {
        var inputParamNames = new HashSet<string>(CurrentWorkflowInputParameters.Select(p => p.Name ?? ""), StringComparer.OrdinalIgnoreCase);
        var filtered = DiscoveredVariables
            .Where(v => !IsNodeOutputVariable(v.Name))
            .Where(v => v.Type != VariableType.Custom || inputParamNames.Contains(v.Name))
            .ToList();

        if (!string.IsNullOrWhiteSpace(VariablePickerSearch))
        {
            var q = VariablePickerSearch.Trim();
            filtered = filtered.Where(v =>
                    v.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (v.SourceNode ?? "").Contains(q, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return filtered.GroupBy(v => v.Type switch
        {
            VariableType.GlobalAttribute => LDesigner["VarTypeAttributes"].Value,
            VariableType.System => LDesigner["VarTypeSystem"].Value,
            VariableType.User => LDesigner["VarTypeUser"].Value,
            VariableType.Custom => LDesigner["VarTypeParams"].Value,
            _ => LDesigner["VarTypeOther"].Value
        }).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Name).ToList());
    }

    private static string GetVariableTypeCss(VariableType type) => type switch
    {
        VariableType.GlobalAttribute => "global",
        VariableType.System => "system",
        VariableType.User => "user",
        VariableType.Custom => "custom",
        _ => "custom"
    };

    private static string GetVariableDisplayName(VariableInfo v) =>
        !string.IsNullOrWhiteSpace(v.SourceNode) ? v.SourceNode : v.Name;

    private static string GetVariableIcon(VariableType type) => type switch
    {
        VariableType.GlobalAttribute => "oi-person",
        VariableType.System => "oi-cog",
        VariableType.User => "oi-person",
        VariableType.Custom => "oi-tag",
        _ => "oi-tag"
    };

    private static string GetNodeIcon(WorkflowNodeModel node)
    {
        return node.NodeType?.ToLowerInvariant() switch
        {
            "start" => "oi-media-play",
            "ask" => "oi-question-mark",
            "message" => "oi-chat",
            "httprequest" => "oi-cloud-download",
            "aigenerate" => "oi-bolt",
            "media" => "oi-image",
            "subworkflow" => "oi-layers",
            "condition" => "oi-fork",
            "wait" => "oi-timer",
            _ => "oi-grid-three-up"
        };
    }

    private void InsertVariable(object dataObject, string propertyName, string variableName)
    {
        var property = dataObject.GetType().GetProperty(propertyName);
        if (property != null)
        {
            var currentValue = property.GetValue(dataObject)?.ToString() ?? "";
            var toInsert = "{{" + VariableNameToStorageForm(variableName) + "}}";
            property.SetValue(dataObject, currentValue + toInsert);
            OnWorkflowChanged();
        }
    }

    private void InsertVariableReplacing(object dataObject, string propertyName, string variableName)
    {
        var property = dataObject.GetType().GetProperty(propertyName);
        if (property != null)
        {
            var toSet = "{{" + VariableNameToStorageForm(variableName) + "}}";
            property.SetValue(dataObject, toSet);
            OnWorkflowChanged();
        }
    }

    private static string VariableNameToStorageForm(string variableName)
    {
        const string nodePrefix = "$node.";
        if (variableName.StartsWith(nodePrefix, StringComparison.OrdinalIgnoreCase))
            return variableName[nodePrefix.Length..];
        return variableName.TrimStart('$');
    }

    #endregion

    #region Helpers

    private Guid? GetNodeIdFromAnchor(Anchor anchor)
    {
        if (anchor.Model is NodeModel node)
            return Guid.Parse(node.Id);

        if (anchor.Model is PortModel port)
            return Guid.Parse(port.Parent.Id);

        return null;
    }

    private (Guid? fromId, Guid? toId) GetEdgeFromTo(WorkflowLinkModel l)
    {
        var sourcePort = l.Source?.Model as PortModel;
        var targetPort = l.Target?.Model as PortModel;
        if (sourcePort != null && targetPort != null)
        {
            var sourceNodeId = Guid.Parse(sourcePort.Parent.Id);
            var targetNodeId = Guid.Parse(targetPort.Parent.Id);
            if (sourcePort.Alignment == PortAlignment.Right && targetPort.Alignment == PortAlignment.Left)
                return (sourceNodeId, targetNodeId);
            if (sourcePort.Alignment == PortAlignment.Left && targetPort.Alignment == PortAlignment.Right)
                return (targetNodeId, sourceNodeId);
        }
        var fromId = GetNodeIdFromAnchor(l.Source);
        var toId = GetNodeIdFromAnchor(l.Target);
        return (fromId, toId);
    }

    private void OnSelectionChanged(SelectableModel model)
    {
        SelectedModel = model.Selected ? (Model)model : null;
        _conditionTypeDropdownOpen = false;
        ApplyNodeDragLocks();
        StateHasChanged();

        if (model.Selected && model is NodeModel nodeModel && nodeModel is WorkflowNodeModel wfNode)
        {
            if (wfNode.Data is IHasRecipient recipientNode && recipientNode.SendToCustomRecipient && recipientNode.CustomRecipientClientChannelId.HasValue)
            {
                var recipientChannelId = recipientNode.CustomRecipientClientChannelId.Value;
                if (!_recipientDetailsCache.ContainsKey(recipientChannelId))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var details = await ClientApiClient.GetClientChannelDetailsAsync(recipientChannelId);
                            if (details != null)
                            {
                                _recipientDetailsCache[recipientChannelId] = details;
                                await InvokeAsync(StateHasChanged);
                            }
                        }
                        catch
                        {
                            // Ignore
                        }
                    });
                }
            }
        }

        if (model.Selected)
        {
            _ = InvokeAsync(async () =>
            {
                try
                {
                    await JSRuntime.InvokeVoidAsync("__releaseDiagramPointerDragDeferred");
                }
                catch (JSDisconnectedException)
                {
                }
            });
        }
    }

    private void ApplyNodeDragLocks()
    {
        if (SelectedModel is NodeModel activeNode)
        {
            foreach (var node in Diagram.Nodes)
                node.Locked = !ReferenceEquals(node, activeNode);
        }
        else if (SelectedModel != null)
        {
            foreach (var node in Diagram.Nodes)
                node.Locked = true;
        }
        else
        {
            foreach (var node in Diagram.Nodes)
                node.Locked = false;
        }
    }

    private void OnSettingsPanelPointerEnter()
    {
        _ = InvokeAsync(async () =>
        {
            try
            {
                await JSRuntime.InvokeVoidAsync("__releaseDiagramPointerDragCore");
            }
            catch (JSDisconnectedException)
            {
            }
        });
    }

    private void OnDragStart(DragEventArgs e, NodeType type) => _draggedType = type;

    private List<NodeToolItem> AllNodeTools =>
    [
        new NodeToolItem(LDesigner["SectionLogic"], LDesigner["NodeLabelStart"], NodeType.Start, NodeToolPaletteSvg.Start, "green"),
        new NodeToolItem(LDesigner["SectionLogic"], LDesigner["NodeLabelTimer"], NodeType.TimerStart, NodeToolPaletteSvg.TimerStart, "green"),
        new NodeToolItem(LDesigner["SectionLogic"], LDesigner["NodeLabelWait"], NodeType.Wait, NodeToolPaletteSvg.Wait, "blue"),
        new NodeToolItem(LDesigner["SectionLogic"], LDesigner["NodeLabelWebhookWait"], NodeType.WebhookWait, NodeToolPaletteSvg.WebhookWait, "blue"),
        new NodeToolItem(LDesigner["SectionLogic"], LDesigner["NodeLabelSubWorkflow"], NodeType.SubWorkflow, NodeToolPaletteSvg.SubWorkflow, "orange"),
        new NodeToolItem(LDesigner["SectionLogic"], LDesigner["NodeLabelOperator"], NodeType.TransferToOperator, NodeToolPaletteSvg.TransferToOperator, "teal"),
        new NodeToolItem(LDesigner["SectionContent"], LDesigner["NodeLabelMessage"], NodeType.Message, NodeToolPaletteSvg.Message, "indigo"),
        new NodeToolItem(LDesigner["SectionContent"], LDesigner["NodeLabelAsk"], NodeType.Ask, NodeToolPaletteSvg.Ask, "indigo"),
        new NodeToolItem(LDesigner["SectionContent"], LDesigner["NodeLabelMedia"], NodeType.Media, NodeToolPaletteSvg.Media, "indigo"),
        new NodeToolItem(LDesigner["SectionAiIntegrations"], LDesigner["NodeLabelHttpRequest"], NodeType.HttpRequest, NodeToolPaletteSvg.HttpRequest, "violet"),
        new NodeToolItem(LDesigner["SectionAiIntegrations"], LDesigner["NodeLabelSetAttribute"], NodeType.SetAttribute, NodeToolPaletteSvg.SetAttribute, "violet"),
        new NodeToolItem(LDesigner["SectionAiIntegrations"], LDesigner["NodeLabelAIGenerate"], NodeType.AIGenerate, NodeToolPaletteSvg.AiGenerate, "violet"),
    ];

    private IEnumerable<NodeToolItem> GetFilteredNodeTools()
    {
        var q = (NodeSearchQuery ?? "").Trim();
        if (string.IsNullOrEmpty(q))
            return AllNodeTools;
        return AllNodeTools.Where(t =>
            t.Label.Contains(q, StringComparison.OrdinalIgnoreCase) ||
            t.Section.Contains(q, StringComparison.OrdinalIgnoreCase));
    }

    private sealed record NodeToolItem(string Section, string Label, NodeType Type, string IconSvg, string IconClass);

    private void OnConditionTypeChanged(WorkflowLinkModel link, string? type)
    {
        if (type == "and")
        {
            link.Condition = new ConditionDefinition { And = [NewEmptyCondition()] };
            link.Label = LDesigner["AndLabel"];
            _conditionTypeDropdownOpen = false;
            StateHasChanged();
            return;
        }
        if (type == "or")
        {
            link.Condition = new ConditionDefinition { Or = [NewEmptyCondition()] };
            link.Label = LDesigner["OrLabel"];
            _conditionTypeDropdownOpen = false;
            StateHasChanged();
            return;
        }
        link.Condition = type switch
        {
            "equals" => new ConditionDefinition { Equals = new EqualsCondition("", "value") },
            "notEquals" => new ConditionDefinition { NotEquals = new NotEqualsCondition("", "value") },
            "contains" => new ConditionDefinition { Contains = new ContainsCondition("", "text") },
            "startsWith" => new ConditionDefinition { StartsWith = new StartsWithCondition("", "") },
            "endsWith" => new ConditionDefinition { EndsWith = new EndsWithCondition("", "") },
            "greaterThan" => new ConditionDefinition { GreaterThan = new GreaterThanCondition("", "0") },
            "lessThan" => new ConditionDefinition { LessThan = new LessThanCondition("", "0") },
            "greaterOrEqual" => new ConditionDefinition { GreaterOrEqual = new GreaterOrEqualCondition("", "0") },
            "lessOrEqual" => new ConditionDefinition { LessOrEqual = new LessOrEqualCondition("", "0") },
            "inList" => new ConditionDefinition { InList = new InListCondition("", "a, b, c") },
            "regex" => new ConditionDefinition { Regex = new RegexMatchCondition("", ".*") },
            "isEmpty" => new ConditionDefinition { IsEmpty = new IsEmptyCondition("") },
            "isNotEmpty" => new ConditionDefinition { IsNotEmpty = new IsNotEmptyCondition("") },
            _ => null
        };
        link.Label = type switch
        {
            "equals" => LDesigner["BadgeEquals"].Value,
            "notEquals" => LDesigner["BadgeNotEquals"].Value,
            "contains" => LDesigner["BadgeContains"].Value,
            "startsWith" => LDesigner["BadgeStartsWith"].Value,
            "endsWith" => LDesigner["BadgeEndsWith"].Value,
            "greaterThan" => LDesigner["BadgeGreaterThan"].Value,
            "lessThan" => LDesigner["BadgeLessThan"].Value,
            "greaterOrEqual" => "≥",
            "lessOrEqual" => "≤",
            "inList" => LDesigner["BadgeInList"].Value,
            "regex" => "Regex",
            "isEmpty" => LDesigner["BadgeIsEmpty"].Value,
            "isNotEmpty" => LDesigner["BadgeIsNotEmpty"].Value,
            _ => ""
        };
        _conditionTypeDropdownOpen = false;
        StateHasChanged();
    }

    private static void SetConditionTypeOn(ConditionDefinition cond, string? type)
    {
        cond.Equals = null;
        cond.NotEquals = null;
        cond.Contains = null;
        cond.StartsWith = null;
        cond.EndsWith = null;
        cond.GreaterThan = null;
        cond.LessThan = null;
        cond.GreaterOrEqual = null;
        cond.LessOrEqual = null;
        cond.InList = null;
        cond.Regex = null;
        cond.IsEmpty = null;
        cond.IsNotEmpty = null;
        cond.And = null;
        cond.Or = null;
        switch (type)
        {
            case "equals": cond.Equals = new EqualsCondition("", "value"); break;
            case "notEquals": cond.NotEquals = new NotEqualsCondition("", "value"); break;
            case "contains": cond.Contains = new ContainsCondition("", "text"); break;
            case "startsWith": cond.StartsWith = new StartsWithCondition("", ""); break;
            case "endsWith": cond.EndsWith = new EndsWithCondition("", ""); break;
            case "greaterThan": cond.GreaterThan = new GreaterThanCondition("", "0"); break;
            case "lessThan": cond.LessThan = new LessThanCondition("", "0"); break;
            case "greaterOrEqual": cond.GreaterOrEqual = new GreaterOrEqualCondition("", "0"); break;
            case "lessOrEqual": cond.LessOrEqual = new LessOrEqualCondition("", "0"); break;
            case "inList": cond.InList = new InListCondition("", "a, b, c"); break;
            case "regex": cond.Regex = new RegexMatchCondition("", ".*"); break;
            case "isEmpty": cond.IsEmpty = new IsEmptyCondition(""); break;
            case "isNotEmpty": cond.IsNotEmpty = new IsNotEmptyCondition(""); break;
        }
    }

    private void OnConditionTypeChangedForSub(WorkflowLinkModel link, ConditionDefinition sub, string? type)
    {
        SetConditionTypeOn(sub, type);
        OnWorkflowChanged();
        StateHasChanged();
    }

    private void ConvertToAnd(WorkflowLinkModel link)
    {
        if (link.Condition == null || link.Condition.IsComposite) return;
        var current = CloneCondition(link.Condition);
        link.Condition = new ConditionDefinition { And = [current, NewEmptyCondition()] };
        link.Label = LDesigner["AndLabel"];
        OnWorkflowChanged();
        StateHasChanged();
    }

    private void ConvertToOr(WorkflowLinkModel link)
    {
        if (link.Condition == null || link.Condition.IsComposite) return;
        var current = CloneCondition(link.Condition);
        link.Condition = new ConditionDefinition { Or = [current, NewEmptyCondition()] };
        link.Label = LDesigner["OrLabel"];
        OnWorkflowChanged();
        StateHasChanged();
    }

    private void AddSubCondition(WorkflowLinkModel link)
    {
        if (link.Condition?.SubConditions == null) return;
        link.Condition.SubConditions.Add(NewEmptyCondition());
        OnWorkflowChanged();
        StateHasChanged();
    }

    private void RemoveSubCondition(WorkflowLinkModel link, int index)
    {
        var list = link.Condition?.SubConditions;
        if (list == null || index < 0 || index >= list.Count) return;
        list.RemoveAt(index);
        if (list.Count == 0)
            link.Condition = null;
        else if (list.Count == 1)
            link.Condition = list[0];
        OnWorkflowChanged();
        StateHasChanged();
    }

    private static ConditionDefinition NewEmptyCondition() =>
        new ConditionDefinition { Equals = new EqualsCondition("", "value") };

    private static ConditionDefinition CloneCondition(ConditionDefinition c)
    {
        var clone = new ConditionDefinition();
        if (c.Equals != null) clone.Equals = new EqualsCondition(c.Equals.Left, c.Equals.Right) { IgnoreCase = c.Equals.IgnoreCase };
        if (c.NotEquals != null) clone.NotEquals = new NotEqualsCondition(c.NotEquals.Left, c.NotEquals.Right) { IgnoreCase = c.NotEquals.IgnoreCase };
        if (c.Contains != null) clone.Contains = new ContainsCondition(c.Contains.Left, c.Contains.Right) { IgnoreCase = c.Contains.IgnoreCase };
        if (c.StartsWith != null) clone.StartsWith = new StartsWithCondition(c.StartsWith.Left, c.StartsWith.Right) { IgnoreCase = c.StartsWith.IgnoreCase };
        if (c.EndsWith != null) clone.EndsWith = new EndsWithCondition(c.EndsWith.Left, c.EndsWith.Right) { IgnoreCase = c.EndsWith.IgnoreCase };
        if (c.GreaterThan != null) clone.GreaterThan = new GreaterThanCondition(c.GreaterThan.Left, c.GreaterThan.Right) { IgnoreCase = c.GreaterThan.IgnoreCase };
        if (c.LessThan != null) clone.LessThan = new LessThanCondition(c.LessThan.Left, c.LessThan.Right) { IgnoreCase = c.LessThan.IgnoreCase };
        if (c.GreaterOrEqual != null) clone.GreaterOrEqual = new GreaterOrEqualCondition(c.GreaterOrEqual.Left, c.GreaterOrEqual.Right) { IgnoreCase = c.GreaterOrEqual.IgnoreCase };
        if (c.LessOrEqual != null) clone.LessOrEqual = new LessOrEqualCondition(c.LessOrEqual.Left, c.LessOrEqual.Right) { IgnoreCase = c.LessOrEqual.IgnoreCase };
        if (c.InList != null) clone.InList = new InListCondition(c.InList.Left, c.InList.Right) { IgnoreCase = c.InList.IgnoreCase };
        if (c.Regex != null) clone.Regex = new RegexMatchCondition(c.Regex.Left, c.Regex.Right) { IgnoreCase = c.Regex.IgnoreCase };
        if (c.IsEmpty != null) clone.IsEmpty = new IsEmptyCondition(c.IsEmpty.Left);
        if (c.IsNotEmpty != null) clone.IsNotEmpty = new IsNotEmptyCondition(c.IsNotEmpty.Left);
        return clone;
    }

    private static BinaryConditionBase? GetSubBinary(ConditionDefinition sub)
    {
        if (sub.Equals != null) return sub.Equals;
        if (sub.NotEquals != null) return sub.NotEquals;
        if (sub.Contains != null) return sub.Contains;
        if (sub.StartsWith != null) return sub.StartsWith;
        if (sub.EndsWith != null) return sub.EndsWith;
        if (sub.GreaterThan != null) return sub.GreaterThan;
        if (sub.LessThan != null) return sub.LessThan;
        if (sub.GreaterOrEqual != null) return sub.GreaterOrEqual;
        if (sub.LessOrEqual != null) return sub.LessOrEqual;
        if (sub.InList != null) return sub.InList;
        if (sub.Regex != null) return sub.Regex;
        return null;
    }

    private static UnaryConditionBase? GetSubUnary(ConditionDefinition sub)
    {
        if (sub.IsEmpty != null) return sub.IsEmpty;
        if (sub.IsNotEmpty != null) return sub.IsNotEmpty;
        return null;
    }

    private static string GetSubConditionType(ConditionDefinition sub)
    {
        if (sub.Equals != null) return "equals";
        if (sub.NotEquals != null) return "notEquals";
        if (sub.Contains != null) return "contains";
        if (sub.StartsWith != null) return "startsWith";
        if (sub.EndsWith != null) return "endsWith";
        if (sub.GreaterThan != null) return "greaterThan";
        if (sub.LessThan != null) return "lessThan";
        if (sub.GreaterOrEqual != null) return "greaterOrEqual";
        if (sub.LessOrEqual != null) return "lessOrEqual";
        if (sub.InList != null) return "inList";
        if (sub.Regex != null) return "regex";
        if (sub.IsEmpty != null) return "isEmpty";
        if (sub.IsNotEmpty != null) return "isNotEmpty";
        return "equals";
    }

    private string GetConditionTypeDisplayLabel(WorkflowLinkModel link)
    {
        if (link.Condition == null) return LDesigner["AlwaysNoCondition"];
        if (link.Condition.And != null && link.Condition.And.Count > 0)
            return string.Format(LDesigner["ConditionAndCount"], link.Condition.And.Count);
        if (link.Condition.Or != null && link.Condition.Or.Count > 0)
            return string.Format(LDesigner["ConditionOrCount"], link.Condition.Or.Count);
        if (link.Condition.Equals != null) return LDesigner["EqualsLabel"];
        if (link.Condition.NotEquals != null) return LDesigner["NotEqualsLabel"];
        if (link.Condition.Contains != null) return LDesigner["ContainsLabel"];
        if (link.Condition.StartsWith != null) return LDesigner["StartsWithLabel"];
        if (link.Condition.EndsWith != null) return LDesigner["EndsWithLabel"];
        if (link.Condition.GreaterThan != null) return LDesigner["GreaterThanLabel"];
        if (link.Condition.LessThan != null) return LDesigner["LessThanLabel"];
        if (link.Condition.GreaterOrEqual != null) return LDesigner["GreaterOrEqualLabel"];
        if (link.Condition.LessOrEqual != null) return LDesigner["LessOrEqualLabel"];
        if (link.Condition.InList != null) return LDesigner["InListLabel"];
        if (link.Condition.Regex != null) return LDesigner["RegexLabel"];
        if (link.Condition.IsEmpty != null) return LDesigner["IsEmptyLabel"];
        if (link.Condition.IsNotEmpty != null) return LDesigner["IsNotEmptyLabel"];
        return LDesigner["AlwaysNoCondition"];
    }

    public IReadOnlyList<(string Value, string Label)> IncomingMessageKindSelectOptions =>
    [
        ("Text", LDesigner["MediaText"]),
        ("Photo", LDesigner["MediaPhoto"]),
        ("Video", LDesigner["MediaVideo"]),
        ("Audio", LDesigner["MediaAudio"]),
        ("Voice", LDesigner["MediaVoice"]),
        ("Document", LDesigner["MediaDocument"]),
        ("Sticker", LDesigner["MediaSticker"]),
        ("Unknown", LDesigner["MediaUnknown"]),
    ];

    private bool IsMessageKindLeftOperand(string? left)
    {
        if (string.IsNullOrWhiteSpace(left)) return false;
        var t = left.Trim();
        if (string.Equals(t, "$message.kind", StringComparison.OrdinalIgnoreCase)) return true;
        if (t.EndsWith(".messageKind", StringComparison.OrdinalIgnoreCase)) return true;
        var normalized = FromDisplayText(t);
        if (NodeInternalVarRegex.IsMatch(normalized))
        {
            var m = NodeInternalVarRegex.Match(normalized);
            if (m.Success && m.Length == normalized.Length &&
                string.Equals(m.Groups["key"].Value, "messageKind", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        var pm = NodeDisplayVarRegexPretty.Match(t);
        return pm.Success && pm.Length == t.Length &&
               string.Equals(pm.Groups["key"].Value, "messageKind", StringComparison.OrdinalIgnoreCase);
    }

    private IReadOnlyList<(string Value, string Label)> ConditionTypeOptions => new List<(string, string)>
    {
        ("none", LDesigner["AlwaysNoCondition"]),
        ("equals", LDesigner["EqualsLabel"]),
        ("notEquals", LDesigner["NotEqualsLabel"]),
        ("contains", LDesigner["ContainsLabel"]),
        ("startsWith", LDesigner["StartsWithLabel"]),
        ("endsWith", LDesigner["EndsWithLabel"]),
        ("greaterThan", LDesigner["GreaterThanLabel"]),
        ("lessThan", LDesigner["LessThanLabel"]),
        ("greaterOrEqual", LDesigner["GreaterOrEqualLabel"]),
        ("lessOrEqual", LDesigner["LessOrEqualLabel"]),
        ("inList", LDesigner["InListLabel"]),
        ("regex", LDesigner["RegexLabel"]),
        ("isEmpty", LDesigner["IsEmptyLabel"]),
        ("isNotEmpty", LDesigner["IsNotEmptyLabel"]),
        ("and", LDesigner["AndOperatorLabel"]),
        ("or", LDesigner["OrOperatorLabel"])
    };

    private void OpenAiModal()
    {
        IsAiModalOpen = true;
    }

    private int GetDiagramNodeCountForAi() => Diagram.Nodes.Count;

    private Task HandleAiSchemaApplied(WorkflowSchema schema)
    {
        if (schema.InputParameters != null)
            CurrentWorkflowInputParameters = schema.InputParameters
                .Select(p => new WorkflowParameterDto { Name = p.Name ?? "", DefaultValue = p.DefaultValue, Description = p.Description }).ToList();
        if (schema.OutputParameters != null)
            CurrentWorkflowOutputParameters = schema.OutputParameters
                .Select(p => new WorkflowParameterDto { Name = p.Name ?? "", DefaultValue = p.DefaultValue, Description = p.Description }).ToList();

        _isRestoring = true;
        try
        {
            ApplySchemaToDiagram(schema);
        }
        finally
        {
            _isRestoring = false;
        }

        _undoStack.Clear();
        _undoIndex = -1;
        PushUndoState();
        RefreshVariables();
        return Task.CompletedTask;
    }

    private void OnDragOver(DragEventArgs e)
    {
    }

    private void OpenClientPicker(IHasRecipient data)
    {
        _activeNodeForPicker = data;
        IsClientPickerOpen = true;
    }

    private void HandleChannelSelected(ClientChannelWithClientDto details)
    {
        if (_activeNodeForPicker != null)
        {
            _activeNodeForPicker.SendToCustomRecipient = true;
            _activeNodeForPicker.CustomRecipientClientChannelId = details.Channel.Id;
            _recipientDetailsCache[details.Channel.Id] = details;
            OnWorkflowChanged();
        }
        IsClientPickerOpen = false;
        _activeNodeForPicker = null;
    }

    private void SetRecipientToSession(IHasRecipient data)
    {
        data.SendToCustomRecipient = false;
        data.CustomRecipientClientChannelId = null;
        OnWorkflowChanged();
    }

    public void Dispose()
    {
        _storagePickerSearchTimer?.Dispose();
        if (Diagram != null)
        {
            Diagram.SelectionChanged -= OnSelectionChanged;
            Diagram.Changed -= OnDiagramChanged;
        }
    }

    #endregion
}