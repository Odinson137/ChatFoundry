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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BlazorClient.Models.Diagram;

namespace BlazorClient.Pages;

// Модели переменных
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

    [Parameter] public Guid WorkflowId { get; set; }

    private BlazorDiagram Diagram { get; set; } = null!;
    private NodeType? _draggedType;
    private Model? SelectedModel { get; set; }

    // Атрибуты компании (для правой панели)
    private List<AttributeDefinitionDto> CompanyAttributeDefinitions { get; set; } = new();

    // Модальное окно «Переменные / Атрибуты / Параметры» (просмотр)
    private bool IsVariablesModalOpen { get; set; }
    private int VariablesModalTab { get; set; } // 0=Атрибуты, 1=Параметры

    private List<VariableInfo> DiscoveredVariables { get; set; } = new();
    private string VariableSearchQuery { get; set; } = "";

    // Поиск узлов в левой панели
    private string NodeSearchQuery { get; set; } = "";

    // Зум канваса
    private double _zoomLevel = 1.0;
    private int ZoomPercent => (int)Math.Round(_zoomLevel * 100);

    // Время последнего сохранения (для подписи «Сохранено N мин назад»)
    private DateTime? LastSavedAt { get; set; }

    /// <summary>Текст «Сохранено N мин назад» или «Не сохранено».</summary>
    private string LastSavedText
    {
        get
        {
            if (LastSavedAt == null) return "Не сохранено";
            var diff = DateTime.UtcNow - LastSavedAt.Value;
            if (diff.TotalMinutes < 1) return "Сохранено только что";
            if (diff.TotalMinutes < 60) return $"Сохранено {(int)diff.TotalMinutes} мин назад";
            return $"Сохранено {(int)diff.TotalHours} ч назад";
        }
    }

    // Undo/Redo
    private readonly List<(string N, string E, string L)> _undoStack = new();
    private int _undoIndex = -1;
    private bool _isRestoring;
    private const int MaxUndoSteps = 30;

    // Ввод @ для выбора переменной (вставка на место @, в т.ч. в середине текста)
    private object? _atMentionTargetObj;
    private string? _atMentionTargetProp;
    private string _atMentionFullValue = "";
    private int _atMentionIndex;

    // Переменные - модальное окно выбора
    private bool IsVariablePickerOpen { get; set; }
    private string VariablePickerSearch { get; set; } = "";
    private WorkflowNodeModel? VariablePickerSelectedNode { get; set; }
    private Action<string>? OnVariableSelected { get; set; }

    // JSON-меню (открыто/закрыто)
    private bool _jsonMenuOpen;

    // Выпадающий список «Тип условия» на связи (открыт/закрыт)
    private bool _conditionTypeDropdownOpen;

    // Модальное окно выбора файла из хранилища
    private bool IsStoragePickerOpen { get; set; }
    private MediaNodeData? StoragePickerTarget { get; set; }
    private List<FileInfoDto> StoragePickerFiles { get; set; } = [];
    private bool StoragePickerLoading { get; set; }

    // Список workflow для выбора в блоке «Процесс»
    private List<WorkflowListItem>? AvailableWorkflows { get; set; }

    // Модальное окно выбора процесса (поиск + пагинация)
    private bool IsProcessPickerOpen { get; set; }
    private SubWorkflowNodeData? ProcessPickerTarget { get; set; }
    private List<WorkflowListItem> ProcessPickerItems { get; set; } = [];
    private string ProcessPickerSearch { get; set; } = "";
    private bool ProcessPickerHasNext { get; set; }
    private bool ProcessPickerHasPrev { get; set; }
    private string? ProcessPickerEndCursor { get; set; }
    private string? ProcessPickerStartCursor { get; set; }
    private bool ProcessPickerLoading { get; set; }

    /// <summary>Входные и выходные параметры текущего процесса (редактируются во вкладке «Параметры»).</summary>
    private List<WorkflowParameterDto> CurrentWorkflowInputParameters { get; set; } = [];
    private List<WorkflowParameterDto> CurrentWorkflowOutputParameters { get; set; } = [];

    // Pretty отображение автопеременных ($node.{guid}.*) в текстовых полях:
    // в модели храним GUID-версию, в UI показываем «Название блока · переменная».
    private readonly Dictionary<Guid, string> _nodeTitleById = new();
    private readonly Dictionary<Guid, string> _nodePrefixById = new();
    private readonly Dictionary<string, List<Guid>> _nodeIdsByTitle = new(StringComparer.OrdinalIgnoreCase);

    protected override async Task OnInitializedAsync()
    {
        InitializeDiagram();
        await LoadWorkflowData();
        await LoadCompanyAttributes();
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

    /// <summary>
    /// Ключи атрибутов для выпадающего списка в блоке «Атрибут»: базовые + из компании, без дубликатов.
    /// </summary>
    /// <summary>Ключи для вкладки Атрибуты в модалке: базовые + из компании, без дубликатов.</summary>
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

    private static string GetAttributeDisplayName(string key) => key?.ToLowerInvariant() switch
    {
        "name" => "Имя",
        "username" => "Username",
        "phone" => "Телефон",
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
                Factory = (diagram, source, targetAnchor) =>
                {
                    Anchor sourceAnchor = source is PortModel port
                        ? new SinglePortAnchor(port)
                        : new ShapeIntersectionAnchor((NodeModel)source);

                    return new WorkflowLinkModel(sourceAnchor, targetAnchor) { Color = "gray", Width = 1 };
                }
            }
        };

        Diagram = new BlazorDiagram(options);
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
            _loadError = "Workflow не найден.";
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
            _loadError = $"Не удалось загрузить схему: {ex.Message}";
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
        
        RefreshNodeVariableCache();
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
            await using var stream = file.OpenReadStream(maxAllowedSize: 2 * 1024 * 1024); // 2 MB
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
            await JSRuntime.InvokeVoidAsync("alert", $"Ошибка загрузки JSON: {ex.Message}");
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

        // Направление схемы: слева направо (вход Left, выход Right)
        switch (type.ToLower())
        {
            case "start":
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Right)); // только выход вправо
                break;
            case "end":
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Left)); // только вход слева
                break;
            case "condition":
            case "aifilter":
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Left));  // вход
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Right)); // выход 1
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Right)); // выход 2
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
            NodeType.Start => "Старт",
            NodeType.End => "Конец",
            NodeType.Message => "Сообщение",
            NodeType.Ask => "Вопрос",
            NodeType.Condition => "Условие",
            NodeType.Wait => "Задержка",
            NodeType.SetAttribute => "Атрибут",
            NodeType.HttpRequest => "API запрос",
            NodeType.AIFilter => "AI Фильтр",
            NodeType.AIGenerate => "AI Текст",
            NodeType.Media => "Медиа",
            NodeType.SubWorkflow => "Процесс",
            _ => "Блок"
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
            StateHasChanged();
        }
    }

    private void AddAskButton(AskNodeData askData)
    {
        if (askData == null) return;
        askData.Ui ??= new AskUiData();
        askData.Ui.Buttons.Add(new AskButtonData { Text = "", Value = "" });
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
        StoragePickerFiles = [];
        StateHasChanged();
        try
        {
            StoragePickerFiles = await FileApiClient.ListFilesAsync(workflowId: WorkflowId);
        }
        catch
        {
            StoragePickerFiles = [];
        }
        finally
        {
            StoragePickerLoading = false;
            StateHasChanged();
        }
    }

    private void CloseStoragePicker()
    {
        IsStoragePickerOpen = false;
        StoragePickerTarget = null;
        StoragePickerFiles = [];
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
            await using var stream = file.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024); // 20 MB
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

    private static readonly List<(string Name, string Description)> GlobalAttributeVariables =
    [
        ("$global.name", "Имя"),
        ("$global.username", "Username"),
        ("$global.phone", "Телефон"),
        ("$global.email", "Email")
    ];

    private void RefreshVariables()
    {
        var variables = new Dictionary<string, VariableInfo>(StringComparer.OrdinalIgnoreCase);

        // Атрибуты ($global.*)
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

        // Входные параметры процесса (хранятся в сессии без $, но в UI показываем как $param)
        foreach (var p in CurrentWorkflowInputParameters.Where(p => !string.IsNullOrWhiteSpace(p.Name)))
        {
            var name = NormalizeVariableName(p.Name);
            if (!variables.ContainsKey(name))
            {
                variables[name] = new VariableInfo
                {
                    Name = name,
                    Type = VariableType.Custom,
                    SourceNode = string.IsNullOrWhiteSpace(p.Description) ? "Входной параметр процесса" : p.Description
                };
            }
        }

        // Автопеременные по GUID блока: $node.{guid}.output, $node.{guid}.statusCode, ...
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

        // Использования переменных в текстовых полях нод
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
                        if (!string.IsNullOrWhiteSpace(b.Value))
                            TrackUsage(variables, ExtractVariables(b.Value), nodeTitle);
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

        // Использования в условиях на линках
        foreach (var link in Diagram.Links.Cast<WorkflowLinkModel>())
        {
            const string usageNode = "Условие на линке";

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

    private static IReadOnlyList<(string Key, string Display)> GetAutoVariablesForNode(WorkflowNodeModel node)
    {
        if (!Guid.TryParse(node.Id, out var id))
            return [];

        return node.NodeType?.ToLowerInvariant() switch
        {
            "start" => [($"$node.{id}.output", "Payload (старт)")],
            "ask" => [($"$node.{id}.output", "Ответ пользователя")],
            "aigenerate" => [($"$node.{id}.output", "Результат AI")],
            "httprequest" => [($"$node.{id}.output", "Тело ответа (response body)"),
                              ($"$node.{id}.statusCode", "Статус-код (statusCode)"),
                              ($"$node.{id}.success", "Успех запроса (true/false)")],
            _ => []
        };
    }

    /// <summary>Для пикера переменных: из полного ключа (например $node.guid.output) возвращает только имя переменной (output).</summary>
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

            var title = node.Title ?? node.NodeType ?? "Блок";
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

    // Match both short form {{guid.output}} and legacy {{node.guid.output}} / {{$node.guid.output}}
    private static readonly Regex NodeInternalVarRegex =
        new(@"\{\{(?:\$?node\.)?(?<guid>[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12})\.(?<key>[a-zA-Z0-9_]+)\}\}",
            RegexOptions.Compiled);

    // Новый формат без id: «{{Старт · output}}»
    private static readonly Regex NodeDisplayVarRegexPretty =
        new(@"\{\{(?<label>[^·]+)·(?<key>[a-zA-Z0-9_]+)\}\}", RegexOptions.Compiled);

    // Старый формат (обратная совместимость): «{{Старт#ec11eb15.output}}»
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

        // Сначала новый формат «{{Старт · output}}»
        var step1 = NodeDisplayVarRegexPretty.Replace(displayText, m =>
        {
            var label = m.Groups["label"].Value.Trim();
            var key = m.Groups["key"].Value;
            var id = ResolveGuidByTitle(label, key);
            if (id == null)
                return m.Value;
            return "{{" + id.Value.ToString("D") + "." + key + "}}";
        });

        // Затем старый формат «{{Старт#ec11eb15.output}}» для обратной совместимости
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

        // Несколько нод с одним названием — берём первую, у которой есть такая переменная
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

        // 1) точное совпадение с вычисленным уникальным префиксом
        foreach (var (id, p) in _nodePrefixById)
        {
            if (string.Equals(p, prefix, StringComparison.OrdinalIgnoreCase))
                return id;
        }

        // 2) fallback: starts-with по полному guid N
        foreach (var id in _nodeTitleById.Keys)
        {
            var n = id.ToString("N");
            if (n.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return id;
        }

        return null;
    }

    private static string SanitizeNodeTitleForToken(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return "Блок";

        var t = title.Trim();
        t = t.Replace("{", " ").Replace("}", " ").Replace("#", " ").Replace("\r", " ").Replace("\n", " ");
        t = Regex.Replace(t, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(t) ? "Блок" : t;
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
            VariableType.GlobalAttribute => "Атрибуты ($global.*)",
            VariableType.System => "Системные",
            VariableType.User => "Пользователь",
            VariableType.Custom => "Переменные",
            _ => "Другие"
        }).ToDictionary(g => g.Key, g => g.ToList());
    }

    /// <summary>Только переменные (без атрибутов $global.*) для вкладки «Переменные».</summary>
    private Dictionary<string, List<VariableInfo>> GetGroupedVariablesOnlyVariables()
    {
        var onlyVars = DiscoveredVariables.Where(v => v.Type != VariableType.GlobalAttribute).ToList();
        var filtered = string.IsNullOrWhiteSpace(VariableSearchQuery)
            ? onlyVars
            : onlyVars.Where(v => v.Name.Contains(VariableSearchQuery, StringComparison.OrdinalIgnoreCase)).ToList();

        return filtered.GroupBy(v => v.Type switch
        {
            VariableType.System => "Системные",
            VariableType.User => "Пользователь",
            VariableType.Custom => "Переменные",
            _ => "Другие"
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

    /// <summary>Обновляет поле из e.Value; при вводе @ (в любом месте) открывает выбор переменной, вставка на место @.</summary>
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

        var nodes = Diagram.Nodes.Cast<WorkflowNodeModel>().ToList();
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

    /// <summary>Переменные выходов блоков ($node.guid.output или $guid.output) — показываются только при выборе блока, не в «Параметры и прочее».</summary>
    private static bool IsNodeOutputVariable(string varName)
    {
        if (string.IsNullOrEmpty(varName))
            return false;
        if (varName.StartsWith("$node.", StringComparison.OrdinalIgnoreCase))
            return true;
        // Короткий формат из текста: $guid.output
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
            VariableType.GlobalAttribute => "Атрибуты ($global.*)",
            VariableType.System => "Системные",
            VariableType.User => "Пользователь",
            VariableType.Custom => "Параметры",
            _ => "Другие"
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

    /// <summary>Устанавливает значение поля только выбранной переменной (предыдущее содержимое удаляется).</summary>
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

    /// <summary>Strips $node. prefix so we store only {{guid.output}} in the DB.</summary>
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

    /// <summary>
    /// Определяет направление ребра по типам портов: From = узел с выходом (Right), To = узел с входом (Left).
    /// Так связь сохраняется одинаково независимо от того, от какого блока начали тянуть линию.
    /// </summary>
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
        StateHasChanged();
    }

    private void OnDragStart(DragEventArgs e, NodeType type) => _draggedType = type;

    private static readonly List<NodeToolItem> AllNodeTools =
    [
        new NodeToolItem("Логика", "Старт", NodeType.Start, "oi-media-play", "green"),
        new NodeToolItem("Логика", "Условие", NodeType.Condition, "oi-fork", "orange"),
        new NodeToolItem("Логика", "Ожидание", NodeType.Wait, "oi-timer", "blue"),
        new NodeToolItem("Логика", "Процесс", NodeType.SubWorkflow, "oi-layers", "orange"),
        new NodeToolItem("Контент", "Сообщение", NodeType.Message, "oi-chat", "indigo"),
        new NodeToolItem("Контент", "Вопрос", NodeType.Ask, "oi-question-mark", "indigo"),
        new NodeToolItem("Контент", "Медиа", NodeType.Media, "oi-image", "indigo"),
        new NodeToolItem("AI и интеграции", "API Запрос", NodeType.HttpRequest, "oi-cloud-download", "violet"),
        new NodeToolItem("AI и интеграции", "Атрибут", NodeType.SetAttribute, "oi-person", "violet"),
        new NodeToolItem("AI и интеграции", "AI Фильтр", NodeType.AIFilter, "oi-eye", "violet"),
        new NodeToolItem("AI и интеграции", "AI Текст", NodeType.AIGenerate, "oi-bolt", "violet"),
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

    private sealed record NodeToolItem(string Section, string Label, NodeType Type, string Icon, string IconClass);

    private void OnConditionTypeChanged(WorkflowLinkModel link, string? type)
    {
        if (type == "and")
        {
            link.Condition = new ConditionDefinition { And = [NewEmptyCondition()] };
            link.Label = "И";
            _conditionTypeDropdownOpen = false;
            StateHasChanged();
            return;
        }
        if (type == "or")
        {
            link.Condition = new ConditionDefinition { Or = [NewEmptyCondition()] };
            link.Label = "ИЛИ";
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
            "equals" => "Равно",
            "notEquals" => "Не равно",
            "contains" => "Содержит",
            "startsWith" => "Начинается с",
            "endsWith" => "Заканчивается на",
            "greaterThan" => "Больше",
            "lessThan" => "Меньше",
            "greaterOrEqual" => "≥",
            "lessOrEqual" => "≤",
            "inList" => "В списке",
            "regex" => "Regex",
            "isEmpty" => "Пусто",
            "isNotEmpty" => "Не пусто",
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
        link.Label = "И";
        OnWorkflowChanged();
        StateHasChanged();
    }

    private void ConvertToOr(WorkflowLinkModel link)
    {
        if (link.Condition == null || link.Condition.IsComposite) return;
        var current = CloneCondition(link.Condition);
        link.Condition = new ConditionDefinition { Or = [current, NewEmptyCondition()] };
        link.Label = "ИЛИ";
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

    /// <summary>Текущая подпись типа условия для отображения в кнопке выпадающего списка.</summary>
    private static string GetConditionTypeDisplayLabel(WorkflowLinkModel link)
    {
        if (link.Condition == null) return "Без условия (Всегда)";
        if (link.Condition.And != null && link.Condition.And.Count > 0)
            return $"И ({link.Condition.And.Count} условий)";
        if (link.Condition.Or != null && link.Condition.Or.Count > 0)
            return $"ИЛИ ({link.Condition.Or.Count} условий)";
        if (link.Condition.Equals != null) return "Равно (==)";
        if (link.Condition.NotEquals != null) return "Не равно (!=)";
        if (link.Condition.Contains != null) return "Содержит";
        if (link.Condition.StartsWith != null) return "Начинается с";
        if (link.Condition.EndsWith != null) return "Заканчивается на";
        if (link.Condition.GreaterThan != null) return "Больше (>)";
        if (link.Condition.LessThan != null) return "Меньше (<)";
        if (link.Condition.GreaterOrEqual != null) return "Больше или равно (≥)";
        if (link.Condition.LessOrEqual != null) return "Меньше или равно (≤)";
        if (link.Condition.InList != null) return "В списке";
        if (link.Condition.Regex != null) return "Регулярное выражение";
        if (link.Condition.IsEmpty != null) return "Пусто";
        if (link.Condition.IsNotEmpty != null) return "Не пусто";
        return "Без условия (Всегда)";
    }

    private static readonly IReadOnlyList<(string Value, string Label)> ConditionTypeOptions = new List<(string, string)>
    {
        ("none", "Без условия (Всегда)"),
        ("equals", "Равно (==)"),
        ("notEquals", "Не равно (!=)"),
        ("contains", "Содержит"),
        ("startsWith", "Начинается с"),
        ("endsWith", "Заканчивается на"),
        ("greaterThan", "Больше (>)"),
        ("lessThan", "Меньше (<)"),
        ("greaterOrEqual", "Больше или равно (≥)"),
        ("lessOrEqual", "Меньше или равно (≤)"),
        ("inList", "В списке"),
        ("regex", "Регулярное выражение"),
        ("isEmpty", "Пусто"),
        ("isNotEmpty", "Не пусто"),
        ("and", "И (несколько условий)"),
        ("or", "ИЛИ (несколько условий)")
    };

    private void OnDragOver(DragEventArgs e)
    {
    }

    public void Dispose()
    {
        if (Diagram != null)
        {
            Diagram.SelectionChanged -= OnSelectionChanged;
            Diagram.Changed -= OnDiagramChanged;
        }
    }

    #endregion
}