using BlazorClient.Configuration;
using BlazorClient.Models.DTO;
using BlazorClient.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace BlazorClient.Pages;

public partial class LiveChat : IDisposable
{
    [Inject] private IJSRuntime JS { get; set; } = null!;
    private ElementReference _messagesContainer;
    private string messageText = "";
    private List<ChatMessage> messages = [];

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
        }
        catch { }
    }

    private void SelectChat(Guid chatId)
    {
        State.SelectedChatId = chatId;
        messages.Clear();
    }

    private async Task SendMessage()
    {
        if (string.IsNullOrEmpty(messageText) || State.SelectedChatId == null) return;

        var text = messageText;
        messageText = "";

        messages.Add(new ChatMessage(text, true, DateTime.UtcNow));
        await ScrollToBottom();

        try
        {
            await ApiClient.SendLiveChatMessageAsync(State.SelectedChatId.Value, text);
        }
        catch
        {
            messages.Add(new ChatMessage("(ошибка отправки)", false, DateTime.UtcNow));
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

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter") await SendMessage();
    }

    private void HandleNewChatInQueue(Guid id, string clientId, string clientName, string channel, string preview)
    {
        State.AddOrUpdateFromSignalR(id, clientId, clientName, channel, preview);
        InvokeAsync(StateHasChanged);
    }

    private void HandleMessageReceived(Guid id, string direction, string payload, string messageKind, DateTime timestamp)
    {
        if (State.SelectedChatId != id) return;
        messages.Add(new ChatMessage(payload, false, timestamp));
        InvokeAsync(async () =>
        {
            StateHasChanged();
            await ScrollToBottom();
        });
    }

    private void HandleChatTaken(Guid id)
    {
        State.MarkChatTaken(id);
        InvokeAsync(StateHasChanged);
    }

    private void HandleChatClosed(Guid id)
    {
        State.RemoveChat(id);
        InvokeAsync(StateHasChanged);
    }

    private void HandleMessageDelivered(Guid id)
    {
        InvokeAsync(StateHasChanged);
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

    private record ChatMessage(string Text, bool IsFromOperator, DateTime Timestamp);
}
