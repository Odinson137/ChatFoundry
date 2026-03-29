using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;

namespace BlazorClient.Models.Diagram;

public class WorkflowNodeModel : NodeModel
{
    public string NodeType { get; }
    public NodeData? Data { get; set; }

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
        : base(id?.ToString() ?? Guid.NewGuid().ToString(), parent, alignment, position)
    {
    }
}

public class WorkflowLinkModel : LinkModel
{
    private LinkLabelModel? _labelModel;

    public ConditionDefinition? Condition { get; set; }

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
        : base(id?.ToString() ?? Guid.NewGuid().ToString(), source, target)
    {
        Color = "#94a3b8";
        SelectedColor = "#6366f1";
        Width = 1;
    }
}