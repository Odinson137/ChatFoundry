using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using BlazorClient.Auth;
using BlazorClient.Configuration;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlazorClient.Services;

public class SignalRService
{
    private readonly ILocalStorageService _localStorage;
    private readonly ITokenRefreshService _tokenRefreshService;

    private HubConnection? _connection;

    public event Action<Guid, string, string, string, Guid, string>? OnNewChatInQueue;
    public event Action<Guid, string, string, string, DateTime>? OnMessageReceived;
    public event Action<Guid>? OnChatTaken;
    public event Action<Guid>? OnChatClosed;
    public event Action<Guid>? OnMessageDelivered;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public SignalRService(ILocalStorageService localStorage, ITokenRefreshService tokenRefreshService)
    {
        _localStorage = localStorage;
        _tokenRefreshService = tokenRefreshService;
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

                    return token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                        ? token["Bearer ".Length..].Trim()
                        : token;
                };

                options.HttpMessageHandlerFactory = innerHandler =>
                {
                    return new SignalRAuthHandler(innerHandler, _localStorage, _tokenRefreshService);
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
                var channelId = Guid.Parse(data.GetProperty("channelId").GetString()!);
                var preview = data.GetProperty("preview").GetString()!;
                OnNewChatInQueue?.Invoke(sessionId, clientId, clientName, channel, channelId, preview);
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

internal sealed class SignalRAuthHandler : DelegatingHandler
{
    private readonly ILocalStorageService _localStorage;
    private readonly ITokenRefreshService _tokenRefreshService;

    public SignalRAuthHandler(HttpMessageHandler innerHandler, ILocalStorageService localStorage, ITokenRefreshService tokenRefreshService)
    {
        InnerHandler = innerHandler;
        _localStorage = localStorage;
        _tokenRefreshService = tokenRefreshService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        var newToken = await _tokenRefreshService.TryRefreshTokenAsync();
        if (newToken == null)
        {
            await _tokenRefreshService.ClearAuthAndRedirectAsync();
            return response;
        }

        // Build a retry request with the refreshed token
        var retryRequest = new HttpRequestMessage(request.Method, request.RequestUri);
        foreach (var h in request.Headers)
            retryRequest.Headers.TryAddWithoutValidation(h.Key, h.Value);

        // Update the authorization header
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

        // For negotiate POST with form body, replay the content
        if (request.Content != null)
        {
            var contentBytes = await request.Content.ReadAsByteArrayAsync(cancellationToken);
            var contentHeaders = request.Content.Headers.ToArray();
            var retryContent = new ByteArrayContent(contentBytes);
            foreach (var h in contentHeaders)
                retryContent.Headers.TryAddWithoutValidation(h.Key, h.Value);
            retryRequest.Content = retryContent;
        }

        return await base.SendAsync(retryRequest, cancellationToken);
    }
}
