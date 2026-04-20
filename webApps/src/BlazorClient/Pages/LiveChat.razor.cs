using BlazorClient.Models.DTO;
using BlazorClient.Services;
using BlazorClient.Components;
using BlazorClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorClient.Pages;

public partial class LiveChat : IDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private IClientApiClient ClientApi { get; set; } = null!;
    private ElementReference _messagesContainer;
    private ElementReference _inputField;
    private string messageText = "";
    private List<ChatMessage> messages = [];
    private bool isSending;
    private bool isLoadingMessages;
    private string? sendError;

    // Buffer for SignalR messages received while chat was not selected
    private readonly Dictionary<Guid, List<ChatMessage>> _pendingMessages = new();

    private LiveChatSessionDto? selectedChat
    {
        get => State.GetSelectedChat();
        set { if (value != null) State.SelectedChatId = value.Id; }
    }

    protected override async Task OnInitializedAsync()
    {
        await SignalR.ConnectAsync();

        SignalR.OnNewChatInQueue += HandleNewChatInQueue;
        SignalR.OnMessageReceived += HandleMessageReceived;
        SignalR.OnChatTaken += HandleChatTaken;
        SignalR.OnChatClosed += HandleChatClosed;
        SignalR.OnMessageDelivered += HandleMessageDelivered;

        await LoadChats();
    }

    private async Task LoadChats()
    {
        try
        {
            var queued = await ApiClient.GetLiveChatSessionsAsync("Queued");
            var inProgress = await ApiClient.GetLiveChatSessionsAsync("InProgress");
            State.QueuedChats = queued;
            State.MyChats = inProgress;
        }
        catch
        {
            // silently retry on next SignalR event
        }
    }

    private async Task TakeChat(Guid chatId)
    {
        try
        {
            await ApiClient.TakeLiveChatAsync(chatId);
            State.MarkChatTaken(chatId);
            State.SelectedChatId = chatId;
            messages.Clear();
            await LoadMessageHistory(chatId);
        }
        catch
        {
        }
    }

    private async Task SelectChat(Guid chatId)
    {
        State.SelectedChatId = chatId;
        messages.Clear();
        sendError = null;
        await LoadMessageHistory(chatId);
    }

    private async Task LoadMessageHistory(Guid chatId)
    {
        var chat = State.GetSelectedChat();
        if (chat == null) return;

        isLoadingMessages = true;
        StateHasChanged();

        try
        {
            if (chat.ClientChannelId.HasValue)
            {
                var result = await ClientApi.GetMessagesAsync(chat.ClientChannelId.Value, 50);
                result.Items.Reverse();
                messages = result.Items.Select(m => new ChatMessage(
                    m.Payload ?? "",
                    m.Direction.Equals("OUTGOING", StringComparison.OrdinalIgnoreCase),
                    m.CreatedAt,
                    m.MessageKind
                )).ToList();
            }
        }
        catch
        {
            // History load failed — chat still works via SignalR
        }
        finally
        {
            isLoadingMessages = false;

            // Append any SignalR messages that arrived while we were loading
            if (_pendingMessages.TryGetValue(chatId, out var pending))
            {
                _pendingMessages.Remove(chatId);
                var lastHistoryTime = messages.Count > 0 ? messages[^1].Timestamp : DateTime.MinValue;
                var newMessages = pending.Where(m => m.Timestamp > lastHistoryTime).ToList();
                messages.AddRange(newMessages);
            }

            StateHasChanged();
            await ScrollToBottom();
        }
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrEmpty(messageText) || State.SelectedChatId == null) return;

        var text = messageText.Trim();
        messageText = "";
        isSending = true;
        sendError = null;
        await ResetTextareaHeight();
        StateHasChanged();

        messages.Add(new ChatMessage(text, true, DateTime.UtcNow, "TEXT"));
        StateHasChanged();
        await ScrollToBottom();

        try
        {
            await ApiClient.SendLiveChatMessageAsync(State.SelectedChatId.Value, text);
        }
        catch
        {
            sendError = "Не удалось отправить сообщение";
            messages.Add(new ChatMessage("(ошибка отправки)", false, DateTime.UtcNow, "TEXT"));
        }
        finally
        {
            isSending = false;
            StateHasChanged();
        }
    }

    private async Task CloseChat()
    {
        if (State.SelectedChatId == null) return;
        try
        {
            await ApiClient.CloseLiveChatAsync(State.SelectedChatId.Value);
            State.RemoveChat(State.SelectedChatId.Value);
            messages.Clear();
        }
        catch { }
    }

    private async Task HandleTextInput(ChangeEventArgs e)
    {
        messageText = e.Value?.ToString() ?? "";
        await AutoResizeInput();
        StateHasChanged();
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await SendMessage();
        }
    }

    private async Task AutoResizeInput()
    {
        try
        {
            await JS.InvokeVoidAsync("__cfLcAutoResize", _inputField);
        }
        catch { }
    }

    private async Task ResetTextareaHeight()
    {
        try
        {
            await JS.InvokeVoidAsync("__cfLcResetTextareaHeight", _inputField);
        }
        catch { }
    }

    private void HandleNewChatInQueue(Guid id, string clientId, string clientName, string channel, string preview)
    {
        State.AddOrUpdateFromSignalR(id, clientId, clientName, channel, preview);
        InvokeAsync(StateHasChanged);
    }

    private void HandleMessageReceived(Guid id, string direction, string payload, string messageKind, DateTime timestamp)
    {
        var msg = new ChatMessage(payload, false, timestamp, messageKind);

        if (State.SelectedChatId == id && !isLoadingMessages)
        {
            messages.Add(msg);
            InvokeAsync(async () =>
            {
                StateHasChanged();
                await ScrollToBottom();
            });
        }
        else
        {
            // Buffer for when chat is not selected or history is still loading
            if (!_pendingMessages.ContainsKey(id))
                _pendingMessages[id] = [];
            _pendingMessages[id].Add(msg);

            // If this chat is selected but still loading, the buffer will be
            // merged in LoadMessageHistory's finally block
        }
    }

    private void HandleChatTaken(Guid id)
    {
        State.MarkChatTaken(id);
        InvokeAsync(StateHasChanged);
    }

    private void HandleChatClosed(Guid id)
    {
        State.RemoveChat(id);
        if (State.SelectedChatId == id)
            messages.Clear();
        _pendingMessages.Remove(id);
        InvokeAsync(StateHasChanged);
    }

    private void HandleMessageDelivered(Guid id)
    {
        // Just a delivery confirmation — no message content to display
    }

    private async Task ScrollToBottom()
    {
        try
        {
            await JS.InvokeVoidAsync("scrollToBottom", _messagesContainer);
        }
        catch { }
    }

    public void Dispose()
    {
        SignalR.OnNewChatInQueue -= HandleNewChatInQueue;
        SignalR.OnMessageReceived -= HandleMessageReceived;
        SignalR.OnChatTaken -= HandleChatTaken;
        SignalR.OnChatClosed -= HandleChatClosed;
        SignalR.OnMessageDelivered -= HandleMessageDelivered;
    }

    private record ChatMessage(string Text, bool IsFromOperator, DateTime Timestamp, string MessageKind = "TEXT");

    private static string GetInitials(LiveChatSessionDto chat)
    {
        var name = chat.ClientFirstName;
        if (!string.IsNullOrWhiteSpace(name))
        {
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 2
                ? $"{parts[0][0]}{parts[1][0]}".ToUpper()
                : $"{parts[0][0]}".ToUpper();
        }
        name = chat.ClientUserName;
        if (!string.IsNullOrWhiteSpace(name))
            return name[..1].ToUpper();
        return chat.ExternalUserId?.Length > 0 ? chat.ExternalUserId[..1].ToUpper() : "?";
    }

    private static string FormatDateSeparator(DateTime date)
    {
        var today = DateTime.UtcNow.Date;
        if (date == today) return "Сегодня";
        if (date == today.AddDays(-1)) return "Вчера";
        return date.ToString("dd MMMM yyyy");
    }

    private static string FormatShortTime(DateTime? dt) =>
        dt?.ToString("HH:mm") ?? "";

    private RenderFragment RenderMessages(List<ChatMessage> msgs, LiveChatSessionDto chat)
    {
        DateTime? lastDate = null;
        return builder =>
        {
            foreach (var msg in msgs)
            {
                var msgDate = msg.Timestamp.Date;
                if (lastDate == null || lastDate != msgDate)
                {
                    lastDate = msgDate;
                    builder.OpenElement(0, "div");
                    builder.AddAttribute(1, "class", "cd-chat-date-sep");
                    builder.OpenElement(2, "span");
                    builder.AddContent(3, FormatDateSeparator(msgDate));
                    builder.CloseElement();
                    builder.CloseElement();
                }

                var isOutgoing = msg.IsFromOperator;
                builder.OpenElement(10, "div");
                builder.AddAttribute(11, "class", $"cd-msg {(isOutgoing ? "cd-msg-outgoing" : "cd-msg-incoming")}");

                if (!isOutgoing)
                {
                    builder.OpenElement(20, "div");
                    builder.AddAttribute(21, "class", "cd-msg-avatar");
                    builder.AddContent(22, GetInitials(chat));
                    builder.CloseElement();
                }

                builder.OpenElement(30, "div");
                builder.AddAttribute(31, "class", $"cd-msg-bubble {(isOutgoing ? "cd-msg-bubble-out" : "cd-msg-bubble-in")}");

                builder.OpenComponent<ClientMessageContent>(40);
                builder.AddAttribute(41, "MessageKind", msg.MessageKind);
                builder.AddAttribute(42, "Payload", msg.Text);
                builder.CloseComponent();

                builder.OpenElement(50, "span");
                builder.AddAttribute(51, "class", "cd-msg-time");
                builder.AddContent(52, msg.Timestamp.ToString("HH:mm"));
                builder.CloseElement();

                builder.CloseElement(); // bubble

                if (isOutgoing)
                {
                    builder.OpenElement(60, "div");
                    builder.AddAttribute(61, "class", "cd-msg-avatar cd-msg-avatar-bot");
                    builder.AddAttribute(62, "title", "Оператор");
                    builder.AddContent(63, "O");
                    builder.CloseElement();
                }

                builder.CloseElement(); // msg
            }
        };
    }
}
