using System.Text;
using MassTransit;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Shared.Application.Events;
using Shared.Domain.Enums;
using Shared.Domain.Models;
using SmsService.Interfaces;
using SmsService.Models;

namespace SmsService.Consumers;

public sealed class SendSmsMessageConsumer(
    ISmsSettingsProvider smsSettingsProvider,
    IHttpClientFactory httpClientFactory,
    IOptions<SmsOptions> smsOptions,
    ILogger<SendSmsMessageConsumer> logger)
    : IConsumer<BotOutgoingMessage>
{
    public async Task Consume(ConsumeContext<BotOutgoingMessage> context)
    {
        var message = context.Message;
        if (message.Channel != DefaultChannel.Sms) return;

        if (string.IsNullOrEmpty(message.MessageJson)) return;

        logger.LogInformation("Sending SMS to {To} via channel {ChannelId}", message.ExternalUserId, message.ChannelId);

        var token = await smsSettingsProvider.GetSenderPhoneByChannelIdAsync(message.ChannelId, context.CancellationToken);
        if (string.IsNullOrEmpty(token))
        {
            logger.LogError("Channel settings (token) not configured for channel {ChannelId}", message.ChannelId);
            return;
        }

        string senderPhone;
        string apiKey;

        var parts = token.Split(';', 2);
        if (parts.Length == 2)
        {
            senderPhone = parts[0].Trim();
            apiKey = parts[1].Trim();
        }
        else
        {
            logger.LogError("Invalid SMS channel token format. Expected 'senderPhone;apiKey' for channel {ChannelId}", message.ChannelId);
            return;
        }

        string apiUrl = smsOptions.Value.ApiUrl;

        if (string.IsNullOrEmpty(senderPhone) || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiUrl))
        {
            logger.LogError("Invalid SMS channel settings: senderPhone, apiKey or apiUrl is empty");
            return;
        }

        if (!senderPhone.StartsWith("+") && senderPhone.StartsWith("375"))
        {
            senderPhone = "+" + senderPhone;
        }

        var recipientPhone = message.ExternalUserId.Trim();
        if (!recipientPhone.StartsWith("+") && recipientPhone.StartsWith("375"))
        {
            recipientPhone = "+" + recipientPhone;
        }

        string text = ExtractText(message.MessageJson, message.MessageKind);
        if (string.IsNullOrEmpty(text))
        {
            logger.LogWarning("SMS body is empty, skipping send");
            return;
        }

        var requestDto = new SendSmsRequestDto
        {
            To = recipientPhone,
            Message = text,
            From = senderPhone,
            Channel = "sms"
        };

        var json = JsonConvert.SerializeObject(requestDto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpClient = httpClientFactory.CreateClient("SmsGateway");

        httpClient.DefaultRequestHeaders.Clear();
        httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);

        var response = await httpClient.PostAsync(apiUrl, content, context.CancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorResponse = await response.Content.ReadAsStringAsync(context.CancellationToken);
            logger.LogError("Failed to send SMS to {To}. Status: {Status}, Error: {Error}", requestDto.To, response.StatusCode, errorResponse);
            response.EnsureSuccessStatusCode();
        }

        logger.LogInformation("Successfully sent SMS to {To}", requestDto.To);
    }

    private string ExtractText(string messageJson, MessageKind kind)
    {
        try
        {
            if (kind == MessageKind.Buttons)
            {
                var askPayload = JsonConvert.DeserializeObject<AskMessagePayload>(messageJson);
                return askPayload?.Text ?? string.Empty;
            }

            var textPayload = JsonConvert.DeserializeObject<MessagePayload>(messageJson);
            return textPayload?.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to deserialize message JSON, using raw string: {Json}", messageJson);
            return messageJson;
        }
    }
}
