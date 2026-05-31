namespace BlazorClient.Pages;

internal static class NodeToolPaletteSvg
{
    private const string SvgOpen = """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke-linecap="round" stroke-linejoin="round">""";
    private const string SvgClose = "</svg>";

    public static readonly string Start =
        SvgOpen + """<circle cx="12" cy="12" r="10" stroke="#059669" stroke-width="2"/><polygon points="10 8 16 12 10 16 10 8" fill="#059669" stroke="none"/>""" + SvgClose;

    public static readonly string Wait =
        SvgOpen + """<circle cx="12" cy="12" r="10" stroke="#4f46e5" stroke-width="2"/><path d="M12 6v6l4 2" stroke="#6366f1" stroke-width="2" fill="none"/>""" + SvgClose;

    public static readonly string WebhookWait =
        SvgOpen + """<circle cx="12" cy="12" r="10" stroke="#4f46e5" stroke-width="2"/><path d="M16 16v-1a4 4 0 0 0-4-4H8" stroke="#6366f1" stroke-width="2" fill="none" stroke-linecap="round"/><path d="m11 8-3 3 3 3" stroke="#6366f1" stroke-width="2" fill="none" stroke-linejoin="round" stroke-linecap="round"/>""" + SvgClose;

    public static readonly string SubWorkflow =
        SvgOpen + """<rect x="3" y="3" width="7" height="7" rx="1" stroke="#ea580c" stroke-width="2" fill="#fff7ed"/><rect x="14" y="3" width="7" height="7" rx="1" stroke="#ea580c" stroke-width="2" fill="#fff7ed"/><rect x="8" y="14" width="8" height="7" rx="1" stroke="#c2410c" stroke-width="2" fill="#ffedd5"/>""" + SvgClose;

    public static readonly string Message =
        SvgOpen + """<path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" stroke="#4f46e5" stroke-width="2" fill="#eef2ff"/>""" + SvgClose;

    public static readonly string Ask =
        SvgOpen + """<circle cx="12" cy="12" r="10" stroke="#7c3aed" stroke-width="2" fill="#f5f3ff"/><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3" stroke="#6d28d9" stroke-width="2" fill="none"/><path d="M12 17h.01" stroke="#6d28d9" stroke-width="2"/>""" + SvgClose;

    public static readonly string Media =
        SvgOpen + """<rect x="3" y="3" width="18" height="18" rx="2" stroke="#6366f1" stroke-width="2" fill="#eef2ff"/><circle cx="8.5" cy="8.5" r="1.5" fill="#8b5cf6"/><path d="m21 15-5-5L5 21" stroke="#7c3aed" stroke-width="2" fill="none"/>""" + SvgClose;

    public static readonly string HttpRequest =
        SvgOpen + """<circle cx="12" cy="12" r="10" stroke="#7c3aed" stroke-width="2" fill="#f5f3ff"/><path d="M2 12h20" stroke="#8b5cf6" stroke-width="2"/><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z" stroke="#6d28d9" stroke-width="2" fill="none"/>""" + SvgClose;

    public static readonly string SetAttribute =
        SvgOpen + """<path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2" stroke="#9333ea" stroke-width="2" fill="none"/><circle cx="12" cy="7" r="4" stroke="#a855f7" stroke-width="2" fill="#faf5ff"/>""" + SvgClose;

    public static readonly string AiGenerate =
        """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24"><path fill="#f59e0b" stroke="#d97706" stroke-width="1" stroke-linejoin="round" d="m12 3-1.9 5.8-6 .9 4.5 3.8L6.3 21l5.7-3.4 5.7 3.4-2.2-7.5L21 9.7l-6-.9L12 3Z"/></svg>""";

    public static readonly string End =
        SvgOpen + """<circle cx="12" cy="12" r="10" stroke="#dc2626" stroke-width="2" fill="#fef2f2"/><rect x="9" y="9" width="6" height="6" rx="1" fill="#dc2626" stroke="none"/>""" + SvgClose;

    public static readonly string Condition =
        SvgOpen + """<path d="M12 4l8 8-8 8-8-8z" stroke="#ea580c" stroke-width="2" fill="#fff7ed"/>""" + SvgClose;

    public static readonly string AiFilter =
        SvgOpen + """<path d="M4 5h16l-6 8v6l-4-2v-4z" stroke="#d97706" stroke-width="2" fill="#fffbeb"/>""" + SvgClose;

    public static readonly string Default =
        SvgOpen + """<rect x="4" y="4" width="16" height="16" rx="2" stroke="#64748b" stroke-width="2" fill="#f1f5f9"/>""" + SvgClose;

    public static readonly string TransferToOperator =
        SvgOpen + """<path d="M18 8A6 6 0 0 0 6 8c0 7-3 9-3 9h18s-3-2-3-9" stroke="#0d9488" stroke-width="2" fill="none"/><path d="M13.73 21a2 2 0 0 1-3.46 0" stroke="#0d9488" stroke-width="2" fill="none"/><circle cx="18" cy="5" r="3" stroke="#0d9488" stroke-width="2" fill="#f0fdfa"/><path d="M18.5 4v2" stroke="#0d9488" stroke-width="2" stroke-linecap="round"/><path d="M17.5 5h2" stroke="#0d9488" stroke-width="2" stroke-linecap="round"/>""" + SvgClose;

    public static readonly string TimerStart =
        SvgOpen + """<rect x="3" y="4" width="18" height="16" rx="2" stroke="#059669" stroke-width="2" fill="#ecfdf5"/><path d="M12 8v4l3 2" stroke="#059669" stroke-width="2" fill="none" stroke-linecap="round"/><path d="M3 10h2M19 10h2M12 4v2" stroke="#059669" stroke-width="2" stroke-linecap="round"/>""" + SvgClose;

    public static string GetCanvasVariantClass(string? nodeType) => nodeType?.ToLowerInvariant() switch
    {
        "start" => "workflow-designer-node--start",
        "timerstart" => "workflow-designer-node--start",
        "end" => "workflow-designer-node--end",
        "wait" => "workflow-designer-node--wait",
        "webhookwait" => "workflow-designer-node--wait",
        "message" => "workflow-designer-node--message",
        "ask" => "workflow-designer-node--ask",
        "media" => "workflow-designer-node--media",
        "subworkflow" => "workflow-designer-node--subworkflow",
        "httprequest" => "workflow-designer-node--http",
        "setattribute" => "workflow-designer-node--attribute",
        "aigenerate" => "workflow-designer-node--ai",
        "condition" => "workflow-designer-node--condition",
        "aifilter" => "workflow-designer-node--aifilter",
        "transfertooperator" => "workflow-designer-node--operator",
        _ => "workflow-designer-node--default"
    };

    public static string GetCanvasIconSvg(string? nodeType) => nodeType?.ToLowerInvariant() switch
    {
        "start" => Start,
        "timerstart" => TimerStart,
        "end" => End,
        "wait" => Wait,
        "webhookwait" => WebhookWait,
        "message" => Message,
        "ask" => Ask,
        "media" => Media,
        "subworkflow" => SubWorkflow,
        "httprequest" => HttpRequest,
        "setattribute" => SetAttribute,
        "aigenerate" => AiGenerate,
        "condition" => Condition,
        "aifilter" => AiFilter,
        "transfertooperator" => TransferToOperator,
        _ => Default
    };
}
