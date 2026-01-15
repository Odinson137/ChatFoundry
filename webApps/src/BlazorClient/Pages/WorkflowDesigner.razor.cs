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
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorClient.Pages;

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
            
            var node = CreateNodeInstance(nDef.Type, nDef.Label, position, nDef.Id);
            nodeMap[nDef.Id] = node;
            Diagram.Nodes.Add(node);
        }

        foreach (var eDef in schema.Edges)
        {
            if (nodeMap.TryGetValue(eDef.From, out var source) && nodeMap.TryGetValue(eDef.To, out var target))
            {
                var sourcePort = source.GetPort(PortAlignment.Bottom) ?? source.Ports.FirstOrDefault();
                var targetPort = target.GetPort(PortAlignment.Top) ?? target.Ports.FirstOrDefault();

                if (sourcePort != null && targetPort != null)
                {
                    Diagram.Links.Add(new WorkflowLinkModel(
                        new SinglePortAnchor(sourcePort), 
                        new SinglePortAnchor(targetPort)));
                }
            }
        }
    }

    private NodeModel CreateNodeInstance(string type, string label, Point position, Guid? id = null)
    {
        var node = id.HasValue ? new NodeModel(id.Value.ToString(), position) : new NodeModel(position);
        node.Title = label;

        switch (type.ToLower())
        {
            case "start":
                node.AddPort(PortAlignment.Bottom);
                break;
            case "end":
                node.AddPort(PortAlignment.Top);
                break;
            case "message":
            case "ask":
            case "input":
            case "media":
            case "setvariable":
            case "httprequest":
            case "aigenerate":
                node.AddPort(PortAlignment.Top);
                node.AddPort(PortAlignment.Bottom);
                break;
            case "condition":
            case "aifilter":
                node.AddPort(PortAlignment.Top);
                node.AddPort(PortAlignment.Left);
                node.AddPort(PortAlignment.Right);
                break;
            case "wait":
                node.AddPort(PortAlignment.Top);
                node.AddPort(PortAlignment.Bottom);
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

        var node = CreateNodeInstance(_draggedType.Value.ToString(), label, point);
        Diagram.Nodes.Add(node);
        _draggedType = null;
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
        {
            Diagram.Nodes.Remove(node);
        }
        else if (SelectedModel is BaseLinkModel link)
        {
            Diagram.Links.Remove(link);
        }

        SelectedModel = null;
        StateHasChanged();
    }

    private void OnDragStart(DragEventArgs e, NodeType type)
    {
        _draggedType = type;
        e.DataTransfer.EffectAllowed = "copy";
    }

    private async Task SaveWorkflow()
    {
        // 1. Собираем узлы (Nodes)
        var nodes = Diagram.Nodes.Select(n => new NodeDefinition(
            Guid.Parse(n.Id), 
            n.Title, 
            n.Title, 
            null)).ToList();

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

        Console.WriteLine($"Nodes: {nStr}");
        Console.WriteLine($"Edges: {eStr}");
        Console.WriteLine($"Layout: {lStr}");
    }

    private Guid? GetNodeIdFromAnchor(Blazor.Diagrams.Core.Anchors.Anchor anchor)
    {
        if (anchor.Model is NodeModel node) 
            return Guid.Parse(node.Id);

        if (anchor.Model is PortModel port) 
            return Guid.Parse(port.Parent.Id);

        return null;
    }


    private void OnLinkColorChanged(LinkModel link, ChangeEventArgs e)
    {
        var newColor = e.Value?.ToString();
        if (!string.IsNullOrEmpty(newColor))
        {
            link.Color = newColor;
            link.Refresh();
        }
    }

    private void OnLinkWidthChanged(LinkModel link, ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), out var width))
        {
            link.Width = width;
            link.Refresh();
        }
    }

    private void OnLinkLabelChanged(WorkflowLinkModel link, ChangeEventArgs text)
    {
        link.UpdateLabel(text?.Value?.ToString() ?? string.Empty);
        link.Refresh();
    }

    public void Dispose() => Diagram.SelectionChanged -= OnSelectionChanged;
    private void OnDragOver(DragEventArgs e) { }
}
