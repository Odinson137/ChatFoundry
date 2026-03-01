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
    private int VariablesModalTab { get; set; } // 0=Переменные, 1=Атрибуты, 2=Параметры

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
    private Action<string>? OnVariableSelected { get; set; }

    // JSON-меню (открыто/закрыто)
    private bool _jsonMenuOpen;

    // Файловое хранилище — список для выбора в блоке Медиа
    private List<FileInfoDto>? StorageFiles { get; set; }

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

    protected override async Task OnInitializedAsync()
    {
        InitializeDiagram();
        await LoadWorkflowData();
        await LoadCompanyAttributes();
        RefreshVariables();
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
        StateHasChanged();
    }

    private void OnWorkflowChanged()
    {
        RefreshVariables();
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
    }

    private void PushUndoState()
    {
        var nodes = Diagram.Nodes.Cast<WorkflowNodeModel>().Select(n => new NodeDefinition(
            Guid.Parse(n.Id), n.NodeType, n.Title ?? "", n.Data is EmptyNodeData ? null : n.Data)).ToList();
        var edges = Diagram.Links.Cast<WorkflowLinkModel>().Select(l =>
        {
            var fromId = GetNodeIdFromAnchor(l.Source);
            var toId = GetNodeIdFromAnchor(l.Target);
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
            var fromId = GetNodeIdFromAnchor(l.Source);
            var toId = GetNodeIdFromAnchor(l.Target);

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
            var fromId = GetNodeIdFromAnchor(l.Source);
            var toId = GetNodeIdFromAnchor(l.Target);
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
        else if (data == null && type.ToLower() == "setvariable")
        {
            data = new SetVariableNodeData { Variable = "", Value = "" };
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
            NodeType.SetVariable => "Переменная",
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
            NodeType.SetVariable => new SetVariableNodeData { Variable = "", Value = "" },
            NodeType.SetAttribute => new SetAttributeNodeData { Attribute = "", Value = "" },
            NodeType.HttpRequest => new HttpRequestNodeData { Method = "GET", Headers = new(), Url = "" },
            NodeType.AIGenerate => new AIGenerateNodeData { Prompt = "", Variable = ""},
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
        var toInsert = "{{" + variableName.TrimStart('$') + "}}";
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

    private async Task LoadStorageFiles()
    {
        try
        {
            StorageFiles = await FileApiClient.ListFilesAsync(workflowId: WorkflowId);
            StateHasChanged();
        }
        catch
        {
            StorageFiles = new List<FileInfoDto>();
            StateHasChanged();
        }
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
        var variables = new Dictionary<string, VariableInfo>();

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

        // Входные параметры процесса — доступны как {{имя}} во всех узлах
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

        foreach (var node in Diagram.Nodes.Cast<WorkflowNodeModel>())
        {
            var nodeTitle = node.Title ?? "Unnamed";

            if (node.Data is SetVariableNodeData varData && !string.IsNullOrWhiteSpace(varData.Variable))
            {
                var varName = NormalizeVariableName(varData.Variable);
                if (!variables.ContainsKey(varName))
                {
                    variables[varName] = new VariableInfo
                    {
                        Name = varName,
                        Type = GetVariableType(varName),
                        SourceNode = nodeTitle
                    };
                }

                if (!string.IsNullOrWhiteSpace(varData.Value))
                {
                    var usedVars = ExtractVariables(varData.Value);
                    foreach (var usedVar in usedVars)
                    {
                        EnsureVariableExists(variables, usedVar);
                        variables[usedVar].UsageNodes.Add(nodeTitle);
                    }
                }
            }

            if (node.Data is SetAttributeNodeData attrData && !string.IsNullOrWhiteSpace(attrData.Attribute))
            {
                var attrKey = attrData.Attribute.Trim();
                if (attrKey.StartsWith("$global.", StringComparison.OrdinalIgnoreCase))
                    attrKey = attrKey["$global.".Length..];
                var varName = "$global." + attrKey;
                if (!variables.ContainsKey(varName))
                {
                    variables[varName] = new VariableInfo
                    {
                        Name = varName,
                        Type = VariableType.GlobalAttribute,
                        SourceNode = nodeTitle
                    };
                }
                if (!string.IsNullOrWhiteSpace(attrData.Value))
                {
                    var usedVars = ExtractVariables(attrData.Value);
                    foreach (var usedVar in usedVars)
                    {
                        EnsureVariableExists(variables, usedVar);
                        variables[usedVar].UsageNodes.Add(nodeTitle);
                    }
                }
            }

            if (node.Data is MessageNodeData msgData)
            {
                if (!string.IsNullOrWhiteSpace(msgData.Variable))
                {
                    var varName = NormalizeVariableName(msgData.Variable);
                    if (!variables.ContainsKey(varName))
                    {
                        variables[varName] = new VariableInfo
                        {
                            Name = varName,
                            Type = GetVariableType(varName),
                            SourceNode = nodeTitle
                        };
                    }
                }
    
                if (!string.IsNullOrWhiteSpace(msgData.Text))
                {
                    var usedVars = ExtractVariables(msgData.Text);
                    foreach (var varName in usedVars)
                    {
                        EnsureVariableExists(variables, varName);
                        if (!variables[varName].UsageNodes.Contains(nodeTitle))
                        {
                            variables[varName].UsageNodes.Add(nodeTitle);
                        }
                    }
                }
            }
            
            if (node.Data is AskNodeData askData)
            {
                if (!string.IsNullOrWhiteSpace(askData.Variable))
                {
                    var varName = NormalizeVariableName(askData.Variable);
                    if (!variables.ContainsKey(varName))
                    {
                        variables[varName] = new VariableInfo
                        {
                            Name = varName,
                            Type = GetVariableType(varName),
                            SourceNode = nodeTitle
                        };
                    }
                }

                if (!string.IsNullOrWhiteSpace(askData.Text))
                {
                    var usedVars = ExtractVariables(askData.Text);
                    foreach (var varName in usedVars)
                    {
                        EnsureVariableExists(variables, varName);
                        if (!variables[varName].UsageNodes.Contains(nodeTitle))
                        {
                            variables[varName].UsageNodes.Add(nodeTitle);
                        }
                    }
                }
            }
            
            if (node.Data is HttpRequestNodeData httpData)
            {
                // Definitions
                if (!string.IsNullOrWhiteSpace(httpData.ResponseVariable))
                {
                    var varName = NormalizeVariableName(httpData.ResponseVariable);
                    if (!variables.ContainsKey(varName))
                    {
                        variables[varName] = new VariableInfo { Name = varName, Type = GetVariableType(varName), SourceNode = nodeTitle };
                    }
                }
                if (!string.IsNullOrWhiteSpace(httpData.StatusCodeVariable))
                {
                    var varName = NormalizeVariableName(httpData.StatusCodeVariable);
                    if (!variables.ContainsKey(varName))
                    {
                        variables[varName] = new VariableInfo { Name = varName, Type = GetVariableType(varName), SourceNode = nodeTitle };
                    }
                }

                // Usages
                var urlVars = ExtractVariables(httpData.Url);
                foreach (var varName in urlVars)
                {
                    EnsureVariableExists(variables, varName);
                    if (!variables[varName].UsageNodes.Contains(nodeTitle)) variables[varName].UsageNodes.Add(nodeTitle);
                }

                if (!string.IsNullOrWhiteSpace(httpData.Body))
                {
                    var bodyVars = ExtractVariables(httpData.Body);
                    foreach (var varName in bodyVars)
                    {
                        EnsureVariableExists(variables, varName);
if (!variables[varName].UsageNodes.Contains(nodeTitle)) variables[varName].UsageNodes.Add(nodeTitle);
                    }
                }

                foreach (var header in httpData.Headers)
                {
                    var headerVars = ExtractVariables(header.Value);
                    foreach (var varName in headerVars)
                    {
                        EnsureVariableExists(variables, varName);
                        if (!variables[varName].UsageNodes.Contains(nodeTitle)) variables[varName].UsageNodes.Add(nodeTitle);
                    }
                }
            }
            
            if (node.Data is AIGenerateNodeData aiData)
            {
                // Definition
                if (!string.IsNullOrWhiteSpace(aiData.Variable))
                {
                    var varName = NormalizeVariableName(aiData.Variable);
                    if (!variables.ContainsKey(varName))
                    {
                        variables[varName] = new VariableInfo { Name = varName, Type = GetVariableType(varName), SourceNode = nodeTitle };
                    }
                }

                // Usage in Prompt
                if (!string.IsNullOrWhiteSpace(aiData.Prompt))
                {
                    var promptVars = ExtractVariables(aiData.Prompt);
                    foreach (var varName in promptVars)
                    {
                        EnsureVariableExists(variables, varName);
                        if (!variables[varName].UsageNodes.Contains(nodeTitle))
                        {
                            variables[varName].UsageNodes.Add(nodeTitle);
                        }
                    }
                }
            }

            if (node.Data is MediaNodeData mediaData)
            {
                if (!string.IsNullOrWhiteSpace(mediaData.Caption))
                {
                    var captionVars = ExtractVariables(mediaData.Caption);
                    foreach (var varName in captionVars)
                    {
                        EnsureVariableExists(variables, varName);
                        if (!variables[varName].UsageNodes.Contains(nodeTitle))
                        {
                            variables[varName].UsageNodes.Add(nodeTitle);
                        }
                    }
                }
                if (!string.IsNullOrWhiteSpace(mediaData.Value))
                {
                    var valueVars = ExtractVariables(mediaData.Value);
                    foreach (var varName in valueVars)
                    {
                        EnsureVariableExists(variables, varName);
                        if (!variables[varName].UsageNodes.Contains(nodeTitle))
                        {
                            variables[varName].UsageNodes.Add(nodeTitle);
                        }
                    }
                }
            }
        }

        foreach (var link in Diagram.Links.Cast<WorkflowLinkModel>())
        {
            if (link.Condition?.Equals != null)
            {
                var leftVar = NormalizeVariableName(link.Condition.Equals.Left);
                EnsureVariableExists(variables, leftVar);
                variables[leftVar].UsageNodes.Add("Условие на линке");

                var rightVars = ExtractVariables(link.Condition.Equals.Right);
                foreach (var rv in rightVars)
                {
                    EnsureVariableExists(variables, rv);
                    variables[rv].UsageNodes.Add("Условие на линке");
                }
            }

            if (link.Condition?.Contains != null)
            {
                var leftVar = NormalizeVariableName(link.Condition.Contains.Left);
                EnsureVariableExists(variables, leftVar);
                variables[leftVar].UsageNodes.Add("Условие на линке");

                var rightVars = ExtractVariables(link.Condition.Contains.Right);
                foreach (var rv in rightVars)
                {
                    EnsureVariableExists(variables, rv);
                    variables[rv].UsageNodes.Add("Условие на линке");
                }
            }
        }

        DiscoveredVariables = variables.Values
            .OrderBy(v => v.Type)
            .ThenBy(v => v.Name)
            .ToList();
    }

    private void EnsureVariableExists(Dictionary<string, VariableInfo> variables, string varName)
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

    private VariableType GetVariableType(string varName)
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

    private async Task ZoomIn()
    {
        _zoomLevel = Math.Min(2.0, _zoomLevel + 0.1);
        await ApplyZoom();
        StateHasChanged();
    }

    private async Task ZoomOut()
    {
        _zoomLevel = Math.Max(0.25, _zoomLevel - 0.1);
        await ApplyZoom();
        StateHasChanged();
    }

    private async Task ApplyZoom()
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("window.__designerZoom", "designer-canvas-area", _zoomLevel);
        }
        catch
        {
            // Fallback: библиотека может не поддерживать вызов zoom извне
        }
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
        IsVariablePickerOpen = true;
    }

    private void CloseVariablePicker()
    {
        IsVariablePickerOpen = false;
        OnVariableSelected = null;
    }

    private void SelectVariableFromPicker(string variableName)
    {
        if (_atMentionTargetObj != null && !string.IsNullOrEmpty(_atMentionTargetProp))
        {
            var insert = "{{" + variableName.TrimStart('$') + "}}";
            var newVal = _atMentionFullValue.Substring(0, _atMentionIndex) + insert + _atMentionFullValue.Substring(_atMentionIndex + 1);
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
        var value = e.Value?.ToString() ?? "";
        var prop = dataObject.GetType().GetProperty(propertyName);
        prop?.SetValue(dataObject, value);
        if (value.Contains('@'))
        {
            _atMentionFullValue = value;
            _atMentionIndex = value.LastIndexOf('@');
            _atMentionTargetObj = dataObject;
            _atMentionTargetProp = propertyName;
            ShowVariablePicker(_ => { });
        }
    }

    private List<VariableInfo> GetFilteredVariablesForPicker()
    {
        if (string.IsNullOrWhiteSpace(VariablePickerSearch))
            return DiscoveredVariables;

        return DiscoveredVariables
            .Where(v => v.Name.Contains(VariablePickerSearch, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private Dictionary<string, List<VariableInfo>> GetGroupedVariablesForPicker()
    {
        var filtered = GetFilteredVariablesForPicker();
        return filtered.GroupBy(v => v.Type switch
        {
            VariableType.GlobalAttribute => "Контактные данные и атрибуты",
            VariableType.System => "Системные",
            VariableType.User => "Пользователь",
            VariableType.Custom => "Пользовательские",
            _ => "Другие"
        }).ToDictionary(g => g.Key, g => g.ToList());
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

    private void InsertVariable(object dataObject, string propertyName, string variableName)
    {
        var property = dataObject.GetType().GetProperty(propertyName);
        if (property != null)
        {
            var currentValue = property.GetValue(dataObject)?.ToString() ?? "";
            var toInsert = "{{" + variableName.TrimStart('$') + "}}";
            
            property.SetValue(dataObject, currentValue + toInsert);
            
            OnWorkflowChanged();
        }
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

    private void OnSelectionChanged(SelectableModel model)
    {
        SelectedModel = model.Selected ? (Model)model : null;
        StateHasChanged();
    }

    private void OnDragStart(DragEventArgs e, NodeType type) => _draggedType = type;

    private static readonly List<NodeToolItem> AllNodeTools =
    [
        new NodeToolItem("Логика", "Старт", NodeType.Start, "oi-media-play", "green"),
        new NodeToolItem("Логика", "Условие", NodeType.Condition, "oi-fork", "orange"),
        new NodeToolItem("Логика", "Ожидание", NodeType.Wait, "oi-timer", "blue"),
        new NodeToolItem("Логика", "Процесс", NodeType.SubWorkflow, "oi-layers", "orange"),
        new NodeToolItem("Логика", "Конец", NodeType.End, "oi-media-stop", "red"),
        new NodeToolItem("Контент", "Сообщение", NodeType.Message, "oi-chat", "indigo"),
        new NodeToolItem("Контент", "Вопрос", NodeType.Ask, "oi-question-mark", "indigo"),
        new NodeToolItem("Контент", "Медиа", NodeType.Media, "oi-image", "indigo"),
        new NodeToolItem("AI и интеграции", "API Запрос", NodeType.HttpRequest, "oi-cloud-download", "violet"),
        new NodeToolItem("AI и интеграции", "Переменная", NodeType.SetVariable, "oi-list", "violet"),
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
        switch (type)
        {
            case "equals":
                link.Condition = new ConditionDefinition
                {
                    Equals = new EqualsCondition("$var", "value")
                };
                link.Label = "Равно";
                break;
            case "contains":
                link.Condition = new ConditionDefinition
                {
                    Contains = new ContainsCondition("$var", "text")
                };
                link.Label = "Содержит";
                break;
            default:
                link.Condition = null;
                link.Label = "";
                break;
        }
        StateHasChanged();
    }

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