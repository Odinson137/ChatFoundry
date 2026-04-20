using BlazorClient.Configuration;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorClient.Services;

public class SignalRService
{
    private readonly ILocalStorageService _localStorage;
    private HubConnection? _connection;

    public event Action<Guid, string, string, string, string>? OnNewChatInQueue;
    public event Action<Guid, string, string, string, DateTime>? OnMessageReceived;
    public event Action<Guid>? OnChatTaken;
    public event Action<Guid>? OnChatClosed;
    public event Action<Guid>? OnMessageDelivered;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public SignalRService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task ConnectAsync()
    {
        if (_connection != null)
            return;

        var hubUrl = $"{ApiEndpoints.Api}/notification/hub/livechat";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var token = await _localStorage.GetItemAsync<string>("authToken");
                    if (string.IsNullOrWhiteSpace(token))
                        return null;

                    // SignalR expects raw JWT token without "Bearer " prefix.
                    return token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? token["Bearer ".Length..].Trim()
                        : token;
                };
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On<string, string, string, string, string>("NewChatInQueue",
            (liveChatSessionId, clientId, clientName, channel, preview) =>
            {
                OnNewChatInQueue?.Invoke(Guid.Parse(liveChatSessionId), clientId, clientName, channel, preview);
            });

        _connection.On<string, string, string, string, DateTime>("MessageReceived",
            (liveChatSessionId, direction, payload, messageKind, timestamp) =>
            {
                OnMessageReceived?.Invoke(Guid.Parse(liveChatSessionId), direction, payload, messageKind, timestamp);
            });

        _connection.On<string>("ChatTaken",
            (liveChatSessionId) =>
            {
                OnChatTaken?.Invoke(Guid.Parse(liveChatSessionId));
            });

        _connection.On<string>("ChatClosed",
            (liveChatSessionId) =>
            {
                OnChatClosed?.Invoke(Guid.Parse(liveChatSessionId));
            });

        _connection.On<string>("MessageDelivered",
            (liveChatSessionId) =>
            {
                OnMessageDelivered?.Invoke(Guid.Parse(liveChatSessionId));
            });

        await _connection.StartAsync();
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
