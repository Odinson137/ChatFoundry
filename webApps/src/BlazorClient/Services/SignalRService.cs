using System.Text.Json;
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

        _connection.On<JsonElement>("NewChatInQueue",
            (data) =>
            {
                var sessionId = Guid.Parse(data.GetProperty("liveChatSessionId").GetString()!);
                var clientId = data.GetProperty("clientId").GetString()!;
                var clientName = data.GetProperty("clientName").GetString()!;
                var channel = data.GetProperty("channel").GetString()!;
                var preview = data.GetProperty("preview").GetString()!;
                OnNewChatInQueue?.Invoke(sessionId, clientId, clientName, channel, preview);
            });

        _connection.On<JsonElement>("MessageReceived",
            (data) =>
            {
                var sessionId = Guid.Parse(data.GetProperty("liveChatSessionId").GetString()!);
                var direction = data.GetProperty("direction").GetString()!;
                var payload = data.GetProperty("payload").GetString()!;
                var messageKind = data.GetProperty("messageKind").GetString()!;
                var timestamp = data.GetProperty("timestamp").GetDateTime();
                OnMessageReceived?.Invoke(sessionId, direction, payload, messageKind, timestamp);
            });

        _connection.On<JsonElement>("ChatTaken",
            (data) =>
            {
                var sessionId = Guid.Parse(data.GetProperty("liveChatSessionId").GetString()!);
                OnChatTaken?.Invoke(sessionId);
            });

        _connection.On<JsonElement>("ChatClosed",
            (data) =>
            {
                var sessionId = Guid.Parse(data.GetProperty("liveChatSessionId").GetString()!);
                OnChatClosed?.Invoke(sessionId);
            });

        _connection.On<JsonElement>("MessageDelivered",
            (data) =>
            {
                var sessionId = Guid.Parse(data.GetProperty("liveChatSessionId").GetString()!);
                OnMessageDelivered?.Invoke(sessionId);
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
