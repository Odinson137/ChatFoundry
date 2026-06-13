using BlazorClient.Models.DTO;
using BlazorClient.Components;
using BlazorClient.Interfaces;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace BlazorClient.Pages;

public partial class LiveChat : IDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private IClientApiClient ClientApi { get; set; } = null!;
    [Inject] private IFileApiClient FileApi { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private IStringLocalizer<LiveChat> LChat { get; set; } = null!;
    [SupplyParameterFromQuery(Name = "chatId")]
    private Guid? ChatId { get; set; }

    private ElementReference _messagesContainer;
    private ElementReference _inputField;
    private string messageText = "";
    private List<ChatMessage> messages = [];
    private bool isSending;
    private bool isLoadingMessages;
    private string? sendError;
    private Guid? clientPageId; // resolved ClientId for the header link
    private string? conflictError; // shown when another operator already has the chat

    // File attachment state
    private IBrowserFile? _selectedFile;
    private string? _selectedFileName;
    private string? _selectedFileContentType;

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
        await TryAutoSelectChat();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (ChatId.HasValue)
        {
            await LoadChats();
            await TryAutoSelectChat();
        }
    }

    private async Task TryAutoSelectChat()
    {
        var chatIdToSelect = ChatId ?? State.SelectedChatId;
        var chatExists = State.QueuedChats.Any(c => c.Id == chatIdToSelect)
                         || State.MyChats.Any(c => c.Id == chatIdToSelect);
        if (chatIdToSelect.HasValue && chatExists)
        {
            await SelectChat(chatIdToSelect.Value);
        }
        else if (!chatIdToSelect.HasValue)
        {
            State.SelectedChatId = null;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender && _lastAutoResizedText != messageText)
        {
            _lastAutoResizedText = messageText;
            await AutoResizeInput();
        }
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
        conflictError = null;
        try
        {
            await ApiClient.TakeLiveChatAsync(chatId);
            State.MarkChatTaken(chatId);
            State.SelectedChatId = chatId;
            messages.Clear();
            await LoadMessageHistory(chatId);
        }
        catch (Exception ex)
        {
            var msg = ex.Message;
            if (msg.Contains("not in queue", StringComparison.OrdinalIgnoreCase))
            {
                conflictError = LChat["ChatAlreadyTaken"].Value;
                // Refresh list to reflect current state
                await LoadChats();
            }
        }
    }

    private async Task SelectChat(Guid chatId)
    {
        State.SelectedChatId = chatId;
        messages.Clear();
        sendError = null;
        conflictError = null;
        await LoadMessageHistory(chatId);
    }

    private async Task LoadMessageHistory(Guid chatId)
    {
        var chat = State.GetSelectedChat();
        if (chat == null) return;

        isLoadingMessages = true;
        StateHasChanged();

        // Resolve client page link in background
        _ = ResolveClientPageLinkAsync(chat.ClientChannelId);

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
            else
            {
                var result = await ClientApi.GetMessagesByChannelAsync(chat.ChannelId, chat.ExternalUserId, chat.Channel, 50);
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

    private async Task ResolveClientPageLinkAsync(Guid? clientChannelId)
    {
        if (clientChannelId == null || clientPageId != null) return;
        try
        {
            clientPageId = await ClientApi.GetClientIdByChannelIdAsync(clientChannelId.Value);
            StateHasChanged();
        }
        catch
        {
            // Link won't be shown — non-critical
        }
    }

    private void DismissConflictError()
    {
        conflictError = null;
    }

    private async Task SendMessage()
    {
        var sessionId = State.SelectedChatId;
        if (sessionId == null) return;
        if (string.IsNullOrWhiteSpace(messageText) && _selectedFile == null) return;

        var text = messageText.Trim();
        var hasFile = _selectedFile != null;
        var fileContentType = _selectedFileContentType;
        var fileName = _selectedFileName;
        var fileToUpload = _selectedFile;

        // Clear input state immediately
        messageText = "";
        isSending = true;
        sendError = null;
        ClearAttachment();
        await ResetTextareaAndClearValue();
        StateHasChanged();

        try
        {
            if (hasFile && fileToUpload != null)
            {
                // Upload file first
                var uploadResult = await FileApi.UploadFileAsync(
                    fileToUpload.OpenReadStream(maxAllowedSize: 20 * 1024 * 1024),
                    fileName ?? "file",
                    fileContentType,
                    ct: CancellationToken.None);

                if (uploadResult == null)
                {
                    sendError = LChat["FailedToUploadFile"].Value;
                    return;
                }

                var fileId = uploadResult.Id;
                var messageKind = ContentTypeToMessageKind(fileContentType ?? "application/octet-stream");
                var payloadJson = System.Text.Json.JsonSerializer.Serialize(
                    new { text = fileId, caption = string.IsNullOrWhiteSpace(text) ? null : text });

                // Optimistic UI
                messages.Add(new ChatMessage(payloadJson, true, DateTime.UtcNow, messageKind));
                var previewText = string.IsNullOrWhiteSpace(text)
                    ? $"\U0001F4CE {fileName}"
                    : text;
                State.UpdateLastMessagePreview(sessionId.Value, previewText);
                StateHasChanged();
                await ScrollToBottom();

                // Send via API
                await ApiClient.SendLiveChatMessageAsync(
                    sessionId.Value, fileId, messageKind,
                    string.IsNullOrWhiteSpace(text) ? null : text);
            }
            else
            {
                // Text-only
                messages.Add(new ChatMessage(text, true, DateTime.UtcNow, "TEXT"));
                State.UpdateLastMessagePreview(sessionId.Value, text);
                StateHasChanged();
                await ScrollToBottom();

                await ApiClient.SendLiveChatMessageAsync(sessionId.Value, text);
            }
        }
        catch
        {
            sendError = hasFile ? LChat["FailedToSendFile"].Value : LChat["FailedToSendMessage"].Value;
            messages.Add(new ChatMessage(LChat["SendErrorLabel"].Value, false, DateTime.UtcNow, "TEXT"));
        }
        finally
        {
            isSending = false;
            StateHasChanged();
        }
    }

    private async Task OnFileSelected(InputFileChangeEventArgs e)
    {
        var file = e.File;
        if (file == null) return;

        const long maxFileSize = 20 * 1024 * 1024;
        if (file.Size > maxFileSize)
        {
            sendError = LChat["FileTooLarge"].Value;
            return;
        }

        _selectedFile = file;
        _selectedFileName = file.Name;
        _selectedFileContentType = file.ContentType;
        sendError = null;
        StateHasChanged();
    }

    private void ClearAttachment()
    {
        _selectedFile = null;
        _selectedFileName = null;
        _selectedFileContentType = null;
    }

    private static string ContentTypeToMessageKind(string contentType)
    {
        var ct = contentType.ToLowerInvariant();
        if (ct.StartsWith("image/")) return "PHOTO";
        if (ct.StartsWith("video/")) return "VIDEO";
        if (ct.StartsWith("audio/")) return "AUDIO";
        return "DOCUMENT";
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

    private string? _lastAutoResizedText;

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

    private async Task ResetTextareaAndClearValue()
    {
        try
        {
            await JS.InvokeVoidAsync("__cfLcResetTextareaAndClear", _inputField);
        }
        catch { }
    }

    private void HandleNewChatInQueue(Guid id, string clientId, string clientName, string channel, Guid channelId, string preview)
    {
        State.AddOrUpdateFromSignalR(id, clientId, clientName, channel, channelId, preview);
        InvokeAsync(StateHasChanged);
    }

    private void HandleMessageReceived(Guid id, string direction, string payload, string messageKind, DateTime timestamp)
    {
        var msg = new ChatMessage(payload, false, timestamp, messageKind);

        State.UpdateLastMessagePreview(id, payload);

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

    private string FormatDateSeparator(DateTime date)
    {
        var localDate = date.ToLocalTime().Date;
        var today = DateTime.Today;
        if (localDate == today) return LChat["DateToday"].Value;
        if (localDate == today.AddDays(-1)) return LChat["DateYesterday"].Value;
        return localDate.ToString("dd MMMM yyyy");
    }

    private static string FormatShortTime(DateTime? dt) =>
        dt?.ToLocalTime().ToString("HH:mm") ?? "";

    private static readonly System.Text.RegularExpressions.Regex JsonPayloadRegex = new(@"^\s*\{.*""\s*:\s*""", System.Text.RegularExpressions.RegexOptions.Compiled);



    private string FormatPreview(string? preview)
    {
        if (string.IsNullOrWhiteSpace(preview)) return "";
        var trimmed = preview.Trim();
        if (trimmed.Equals("Transferring to operator...", StringComparison.OrdinalIgnoreCase))
            return LChat["TransferToOperatorDots"].Value;
        if (trimmed.Equals("Transferring to operator", StringComparison.OrdinalIgnoreCase))
            return LChat["TransferToOperator"].Value;
        return JsonPayloadRegex.IsMatch(preview) ? LChat["FilePreviewLabel"].Value : preview;
    }

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
                builder.AddContent(52, msg.Timestamp.ToLocalTime().ToString("HH:mm"));
                builder.CloseElement();

                builder.CloseElement(); // bubble

                if (isOutgoing)
                {
                    builder.OpenElement(60, "div");
                    builder.AddAttribute(61, "class", "cd-msg-avatar cd-msg-avatar-bot");
                    builder.AddAttribute(62, "title", LChat["OperatorLabel"].Value);
                    builder.AddContent(63, "O");
                    builder.CloseElement();
                }

                builder.CloseElement(); // msg
            }
        };
    }
}
