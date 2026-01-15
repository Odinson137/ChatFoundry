using Blazor.Diagrams.Core.Anchors;
using Blazor.Diagrams.Core.Models;

namespace BlazorClient.Models;

public class WorkflowLinkModel : LinkModel 
{
    public object? Condition { get; set; }

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