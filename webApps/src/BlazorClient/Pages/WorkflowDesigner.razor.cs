using Blazor.Diagrams;
using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using Blazor.Diagrams.Core.PathGenerators;
using Blazor.Diagrams.Core.Routers;
using Blazor.Diagrams.Options;
using BlazorClient.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace BlazorClient.Pages
{
    public partial class WorkflowDesigner : IDisposable
    {
        [Parameter] public Guid WorkflowId { get; set; }

        private BlazorDiagram Diagram { get; set; } = null!;
        private NodeType? _draggedType;
        
        private Model? SelectedModel { get; set; }

        protected override void OnInitialized()
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
                        Anchor sourceAnchor;

                        if (source is PortModel port)
                        {
                            sourceAnchor = new SinglePortAnchor(port);
                        }
                        else if (source is NodeModel node)
                        {
                            sourceAnchor = new ShapeIntersectionAnchor(node);
                        }
                        else
                        {
                            throw new InvalidOperationException("Unknown link source type");
                        }

                        var link = new WorkflowLinkModel(sourceAnchor, targetAnchor)
                        {
                            Color = "gray",
                            Width = 1
                        };
                        
                        return link;
                    }
                }
            };

            Diagram = new BlazorDiagram(options);
            
            Diagram.SelectionChanged += OnSelectionChanged;

            var startNode = new NodeModel(new Point(50, 50)) { Title = "Start" };
            startNode.AddPort(PortAlignment.Bottom); 
            Diagram.Nodes.Add(startNode);
        }

        private void OnSelectionChanged(SelectableModel model)
        {
            SelectedModel = model.Selected ? (Model)model : null;
            StateHasChanged();
        }

        private void OnDragStart(DragEventArgs e, NodeType type)
        {
            _draggedType = type;
            e.DataTransfer.EffectAllowed = "copy";
        }

        private void OnDragOver(DragEventArgs e) { }

        private void OnDrop(DragEventArgs e)
        {
            if (_draggedType == null) return;
            var point = Diagram.GetRelativeMousePoint(e.ClientX, e.ClientY);
            CreateNode(_draggedType.Value, point);
            _draggedType = null;
        }

        private void CreateNode(NodeType type, Point position)
        {
            var node = new NodeModel(position);
            switch (type)
            {
                case NodeType.Message:
                    node.Title = "Сообщение";
                    node.AddPort(PortAlignment.Top);
                    node.AddPort(PortAlignment.Bottom);
                    break;
                case NodeType.Filter:
                    node.Title = "Фильтр";
                    node.AddPort(PortAlignment.Top);
                    node.AddPort(PortAlignment.Left);
                    node.AddPort(PortAlignment.Right);
                    break;
                case NodeType.Action:
                    node.Title = "Действие";
                    node.AddPort(PortAlignment.Top);
                    break;
            }
            Diagram.Nodes.Add(node);
        }

        private async Task SaveWorkflow()
        {
            var nodes = Diagram.Nodes.Select(n => new 
            { 
                Id = n.Id, 
                X = n.Position.X, 
                Y = n.Position.Y,
                Type = n.Title 
            });
            
            var links = Diagram.Links.Select(l => new {});
            var schema = new { Nodes = nodes, Links = links };
            var json = System.Text.Json.JsonSerializer.Serialize(schema);
            
            Console.WriteLine(json);
        }

        public void Dispose()
        {
            // Обязательно отписываемся от событий при уничтожении компонента
            Diagram.SelectionChanged -= OnSelectionChanged;
        }
        
        private void OnLinkColorChanged(LinkModel link, ChangeEventArgs e)
        {
            var newColor = e.Value?.ToString();
            if (!string.IsNullOrEmpty(newColor))
            {
                link.Color = newColor;
                link.Refresh(); // Принудительная перерисовка линии
            }
        }

        private void OnLinkWidthChanged(LinkModel link, ChangeEventArgs e)
        {
            if (double.TryParse(e.Value?.ToString(), out var width))
            {
                link.Width = width;
                link.Refresh(); // Принудительная перерисовка линии
            }
        }
        
        private void OnLinkLabelChanged(WorkflowLinkModel link, ChangeEventArgs text) {
            link.UpdateLabel(text?.Value?.ToString() ?? string.Empty);
            link.Refresh();
        }
    }
    
    public class WorkflowLinkModel : LinkModel 
    {
        public object? Condition { get; set; }

        // Конструктор теперь принимает якоря (Anchors)
        public WorkflowLinkModel(Anchor sourceAnchor, Anchor? targetAnchor = null) 
            : base(sourceAnchor, targetAnchor) 
        { 
        }

        public void UpdateLabel(string text) 
        {
            Labels.Clear();
            if (!string.IsNullOrEmpty(text)) 
            {
                AddLabel(text); 
            }
        }
    }
}
