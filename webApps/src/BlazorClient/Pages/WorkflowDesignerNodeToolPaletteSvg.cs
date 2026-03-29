namespace BlazorClient.Pages;

/// <summary>SVG-иконки палитры узлов с явными цветами (не currentColor — корректно с MarkupString и изоляцией стилей).</summary>
internal static class NodeToolPaletteSvg
{
    private const string SvgOpen = """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke-linecap="round" stroke-linejoin="round">""";
    private const string SvgClose = "</svg>";

    /// <summary>Старт — изумрудный.</summary>
    public static readonly string Start =
        SvgOpen + """<circle cx="12" cy="12" r="10" stroke="#059669" stroke-width="2"/><polygon points="10 8 16 12 10 16 10 8" fill="#059669" stroke="none"/>""" + SvgClose;

    /// <summary>Ожидание — индиго.</summary>
    public static readonly string Wait =
        SvgOpen + """<circle cx="12" cy="12" r="10" stroke="#4f46e5" stroke-width="2"/><path d="M12 6v6l4 2" stroke="#6366f1" stroke-width="2" fill="none"/>""" + SvgClose;

    /// <summary>Процесс — оранжевый.</summary>
    public static readonly string SubWorkflow =
        SvgOpen + """<rect x="3" y="3" width="7" height="7" rx="1" stroke="#ea580c" stroke-width="2" fill="#fff7ed"/><rect x="14" y="3" width="7" height="7" rx="1" stroke="#ea580c" stroke-width="2" fill="#fff7ed"/><rect x="8" y="14" width="8" height="7" rx="1" stroke="#c2410c" stroke-width="2" fill="#ffedd5"/>""" + SvgClose;

    /// <summary>Сообщение — индиго.</summary>
    public static readonly string Message =
        SvgOpen + """<path d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z" stroke="#4f46e5" stroke-width="2" fill="#eef2ff"/>""" + SvgClose;

    /// <summary>Вопрос — фиолетовый.</summary>
    public static readonly string Ask =
        SvgOpen + """<circle cx="12" cy="12" r="10" stroke="#7c3aed" stroke-width="2" fill="#f5f3ff"/><path d="M9.09 9a3 3 0 0 1 5.83 1c0 2-3 3-3 3" stroke="#6d28d9" stroke-width="2" fill="none"/><path d="M12 17h.01" stroke="#6d28d9" stroke-width="2"/>""" + SvgClose;

    /// <summary>Медиа — сине-фиолетовый градиент имитацией двух цветов.</summary>
    public static readonly string Media =
        SvgOpen + """<rect x="3" y="3" width="18" height="18" rx="2" stroke="#6366f1" stroke-width="2" fill="#eef2ff"/><circle cx="8.5" cy="8.5" r="1.5" fill="#8b5cf6"/><path d="m21 15-5-5L5 21" stroke="#7c3aed" stroke-width="2" fill="none"/>""" + SvgClose;

    /// <summary>HTTP — виолетовый.</summary>
    public static readonly string HttpRequest =
        SvgOpen + """<circle cx="12" cy="12" r="10" stroke="#7c3aed" stroke-width="2" fill="#f5f3ff"/><path d="M2 12h20" stroke="#8b5cf6" stroke-width="2"/><path d="M12 2a15.3 15.3 0 0 1 4 10 15.3 15.3 0 0 1-4 10 15.3 15.3 0 0 1-4-10 15.3 15.3 0 0 1 4-10z" stroke="#6d28d9" stroke-width="2" fill="none"/>""" + SvgClose;

    /// <summary>Атрибут — пурпурный акцент.</summary>
    public static readonly string SetAttribute =
        SvgOpen + """<path d="M19 21v-2a4 4 0 0 0-4-4H9a4 4 0 0 0-4 4v2" stroke="#9333ea" stroke-width="2" fill="none"/><circle cx="12" cy="7" r="4" stroke="#a855f7" stroke-width="2" fill="#faf5ff"/>""" + SvgClose;

    /// <summary>AI — золотистая звезда на тёмно-фиолетовом.</summary>
    public static readonly string AiGenerate =
        """<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24"><path fill="#f59e0b" stroke="#d97706" stroke-width="1" stroke-linejoin="round" d="m12 3-1.9 5.8-6 .9 4.5 3.8L6.3 21l5.7-3.4 5.7 3.4-2.2-7.5L21 9.7l-6-.9L12 3Z"/></svg>""";
}
