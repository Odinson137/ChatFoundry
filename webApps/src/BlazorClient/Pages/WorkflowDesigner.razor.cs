using Blazor.Diagrams;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Blazor.Diagrams.Core.PathGenerators;
using Blazor.Diagrams.Core.Routers;
using Blazor.Diagrams.Options;
using BlazorClient.Interfaces;
using BlazorClient.Models; // Убедитесь, что эта using-директива присутствует
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json; // Добавьте для работы с JsonElement, если потребуется

namespace BlazorClient.Pages;

public class WorkflowNodeModel : NodeModel
{
    public string NodeType { get; }
    public NodeData? Data { get; set; } // Добавлено свойство Data

    public WorkflowNodeModel(Point? position, string nodeType, string title, Guid? id = null, NodeData? data = null) 
        : base(id?.ToString() ?? Guid.NewGuid().ToString(), position)
    {
        NodeType = nodeType;
        Title = title;
        Data = data;
    }
}

public class WorkflowPortModel : PortModel
{
    public WorkflowPortModel(NodeModel parent, PortAlignment alignment, Point? position = null, Guid? id = null)
        : base(id?.ToString() ?? Guid.NewGuid().ToString(), parent, alignment, position) { }
}

public class WorkflowLinkModel : LinkModel
{
    private LinkLabelModel? _labelModel;

    public string? Label
    {
        get => _labelModel?.Content;
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                if (_labelModel != null)
                {
                    Labels.Remove(_labelModel);
                    _labelModel = null;
                }
            }
            else
            {
                if (_labelModel == null)
                {
                    _labelModel = new LinkLabelModel(this, value, offset: new Point(0, -20));
                    Labels.Add(_labelModel);
                }
                else
                {
                    _labelModel.Content = value;
                }
            }
            Refresh();
        }
    }

    public WorkflowLinkModel(Anchor source, Anchor? target = null, Guid? id = null) 
        : base(id?.ToString() ?? Guid.NewGuid().ToString(), source, target) { }
}


public partial class WorkflowDesigner : IDisposable
{
    [Inject] private IWorkflowApiClient ApiClient { get; set; } = null!;
    [Inject] private IWorkflowSchemaService SchemaService { get; set; } = null!;
    
    [Parameter] public Guid WorkflowId { get; set; }

    private BlazorDiagram Diagram { get; set; } = null!;
    private NodeType? _draggedType;
    private Model? SelectedModel { get; set; }

    protected override async Task OnInitializedAsync()
    {
        InitializeDiagram();
        await LoadWorkflowData();
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
    }

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
            
            // ПЕРЕДАЕМ NodeData В CreateNodeInstance
            var node = CreateNodeInstance(nDef.Type, nDef.Label, position, nDef.Id, nDef.Data);
            nodeMap[nDef.Id] = node;
            Diagram.Nodes.Add(node);
        }

        foreach (var eDef in schema.Edges)
        {
            if (nodeMap.TryGetValue(eDef.From, out var source) && nodeMap.TryGetValue(eDef.To, out var target))
            {
                var sourcePort = source.Ports.FirstOrDefault(p => p.Alignment == PortAlignment.Bottom) ?? source.Ports.FirstOrDefault();
                var targetPort = target.Ports.FirstOrDefault(p => p.Alignment == PortAlignment.Top) ?? target.Ports.FirstOrDefault();

                if (sourcePort != null && targetPort != null)
                {
                    var link = new WorkflowLinkModel(new SinglePortAnchor(sourcePort), new SinglePortAnchor(targetPort));
                    Diagram.Links.Add(link);
                }
            }
        }
    }

    // ИЗМЕНЕНИЕ: Добавлен параметр NodeData? data = null
    private NodeModel CreateNodeInstance(string type, string label, Point position, Guid? id = null, NodeData? data = null)
    {
        if (data == null && type.ToLower() == "message")
        {
            // Изменено на инициализатор
            data = new MessageNodeData { Text = "" };
        }
        else if (data == null) // Если данных нет и это не "Message", используем EmptyNodeData
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
    
        var label = _draggedType.Value switch {
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

        NodeData? initialData = null;
        if (_draggedType.Value == NodeType.Message)
        {
            initialData = new MessageNodeData()
            {
                Text = string.Empty
            }; // Создаем пустой MessageNodeData для нового узла
        }
        // Добавьте логику для других типов узлов, если у них есть начальные данные
        
        var node = CreateNodeInstance(_draggedType.Value.ToString(), label, point, data: initialData);
        Diagram.Nodes.Add(node);
        _draggedType = null;
    }

    private async Task SaveWorkflow()
    {
        var nodes = Diagram.Nodes.Cast<WorkflowNodeModel>().Select(n => new NodeDefinition(
            Guid.Parse(n.Id),
            n.NodeType,
            n.Title,
            n.Data is EmptyNodeData ? null : n.Data)).ToList(); // ИЗМЕНЕНИЕ ЗДЕСЬ: отправляем null, если данные пустые

        var edges = Diagram.Links.Select(l =>
        {
            var fromId = GetNodeIdFromAnchor(l.Source);
            var toId = GetNodeIdFromAnchor(l.Target);

            return (fromId.HasValue && toId.HasValue)
                ? new EdgeDefinition(fromId.Value, toId.Value, null)
                : null;
        })
        .Where(e => e != null)
        .Select(e => e!)
        .ToList();

        var layout = Diagram.Nodes.Select(n => new LayoutDefinition(
            Guid.Parse(n.Id),
            n.Position.X,
            n.Position.Y)).ToList();

        var schema = new WorkflowSchema(nodes, edges, layout);
        var (nStr, eStr, lStr) = SchemaService.Serialize(schema);
        
        await ApiClient.UpdateWorkflowDefinitionsAsync(WorkflowId, nStr, eStr, lStr);
    }

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

    private void DeleteSelectedModel()
    {
        if (SelectedModel == null) return;

        if (SelectedModel is NodeModel node)
            Diagram.Nodes.Remove(node);
        else if (SelectedModel is LinkModel link)
            Diagram.Links.Remove(link);
        
        SelectedModel = null;
    }

    private void OnDragStart(DragEventArgs e, NodeType type) => _draggedType = type;
    
    private void OnLinkColorChanged(LinkModel link, ChangeEventArgs e)
    {
        link.Color = e.Value?.ToString() ?? "gray";
        link.Refresh();
    }
    
    private void OnLinkLabelChanged(WorkflowLinkModel link, ChangeEventArgs e)
    {
        link.Label = e.Value?.ToString();
    }
    
    private void OnDragOver(DragEventArgs e) { }
    
    public void Dispose()
    {
        if (Diagram != null)
            Diagram.SelectionChanged -= OnSelectionChanged;
    }
}
