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
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
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
    Contact,
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

    [Parameter] public Guid WorkflowId { get; set; }

    private BlazorDiagram Diagram { get; set; } = null!;
    private NodeType? _draggedType;
    private Model? SelectedModel { get; set; }

    // Переменные - управление панелью
    private bool IsVariablesPanelOpen { get; set; } = true;
    private List<VariableInfo> DiscoveredVariables { get; set; } = new();
    private string VariableSearchQuery { get; set; } = "";

    // Переменные - модальное окно выбора
    private bool IsVariablePickerOpen { get; set; }
    private string VariablePickerSearch { get; set; } = "";
    private Action<string>? OnVariableSelected { get; set; }

    // Файловое хранилище — список для выбора в блоке Медиа
    private List<FileInfoDto>? StorageFiles { get; set; }

    protected override async Task OnInitializedAsync()
    {
        InitializeDiagram();
        await LoadWorkflowData();
        RefreshVariables();
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
        RefreshVariables();
    }

    private void OnWorkflowChanged()
    {
        RefreshVariables();
        StateHasChanged();
    }

    #region Загрузка и сохранение

    private async Task LoadWorkflowData()
    {
        var data = await ApiClient.GetWorkflowByIdAsync(WorkflowId);
        if (data == null) return;

        var schema = SchemaService.Deserialize(data.NodesDefinition, data.EdgesDefinition, data.LayoutDefinition);
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
                var sourcePort = source.Ports.FirstOrDefault(p => p.Alignment == PortAlignment.Bottom) ??
                                 source.Ports.FirstOrDefault();
                var targetPort = target.Ports.FirstOrDefault(p => p.Alignment == PortAlignment.Top) ??
                                 target.Ports.FirstOrDefault();

                if (sourcePort != null && targetPort != null)
                {
                    var link = new WorkflowLinkModel(new SinglePortAnchor(sourcePort),
                        new SinglePortAnchor(targetPort))
                    {
                        Condition = eDef.Condition,
                        Label = eDef.Label
                    };
                    Diagram.Links.Add(link);
                }
            }
        }
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

        await ApiClient.UpdateWorkflowDefinitionsAsync(WorkflowId, nStr, eStr, lStr);
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
        else if (data == null && type.ToLower() == "media")
        {
            data = new MediaNodeData { SourceType = MediaSourceType.Url };
        }
        else if (data == null)
        {
            data = new EmptyNodeData();
        }

        var node = new WorkflowNodeModel(position, type, label, id, data);

        switch (type.ToLower())
        {
            case "start":
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Bottom));
                break;
            case "end":
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Top));
                break;
            case "condition":
            case "aifilter":
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Top));
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Left));
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Right));
                break;
            default:
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Top));
                node.AddPort(new WorkflowPortModel(node, PortAlignment.Bottom));
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
            NodeType.HttpRequest => "API запрос",
            NodeType.AIFilter => "AI Фильтр",
            NodeType.AIGenerate => "AI Текст",
            NodeType.Media => "Медиа",
            _ => "Блок"
        };

        NodeData? initialData = _draggedType.Value switch
        {
            NodeType.Message => new MessageNodeData { Text = "" },
            NodeType.Ask => new AskNodeData { Text = "" },
            NodeType.SetVariable => new SetVariableNodeData { Variable = "", Value = "" },
            NodeType.HttpRequest => new HttpRequestNodeData { Method = "GET", Headers = new(), Url = "" },
            NodeType.AIGenerate => new AIGenerateNodeData { Prompt = "", Variable = ""},
            NodeType.Media => new MediaNodeData { SourceType = MediaSourceType.Url },
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
                mediaData.Value = result.Key;
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

    private static readonly List<(string Name, string Description)> ContactVariables =
    [
        ("$client.name", "Имя"),
        ("$client.username", "Username"),
        ("$client.phone", "Телефон"),
        ("$client.email", "Email")
    ];

    private void RefreshVariables()
    {
        var variables = new Dictionary<string, VariableInfo>();

        foreach (var (name, description) in ContactVariables)
        {
            variables[name] = new VariableInfo
            {
                Name = name,
                Type = VariableType.Contact,
                SourceNode = description
            };
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
                if (!string.IsNullOrWhiteSpace(mediaData.Value) && mediaData.SourceType == MediaSourceType.Url)
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

        if (lower.StartsWith("$client."))
            return VariableType.Contact;

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
            VariableType.Contact => "var-contact",
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
            VariableType.Contact => "Контакт",
            VariableType.System => "Системные",
            VariableType.User => "Пользователь",
            VariableType.Custom => "Пользовательские",
            _ => "Другие"
        }).ToDictionary(g => g.Key, g => g.ToList());
    }

    #endregion

    #region Работа с переменными - UI Actions

    private void ToggleVariablesPanel()
    {
        IsVariablesPanelOpen = !IsVariablesPanelOpen;
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
        OnVariableSelected?.Invoke(variableName);
        CloseVariablePicker();
        StateHasChanged();
    }

    private List<VariableInfo> GetFilteredVariablesForPicker()
    {
        if (string.IsNullOrWhiteSpace(VariablePickerSearch))
            return DiscoveredVariables;

        return DiscoveredVariables
            .Where(v => v.Name.Contains(VariablePickerSearch, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

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