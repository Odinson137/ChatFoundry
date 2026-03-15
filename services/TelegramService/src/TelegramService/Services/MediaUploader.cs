using System.Net.Http.Headers;
using System.Text.Json;
using Telegram.Bot;
using TelegramService.Interfaces;
using Workflow.Grpc;

namespace TelegramService.Services;

public sealed class MediaUploader(
    IBotTokenProvider botTokenProvider,
    IHttpClientFactory httpClientFactory,
    BotTokenService.BotTokenServiceClient botTokenServiceClient,
    ILogger<MediaUploader> logger) : IMediaUploader
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024;

    public async Task<MediaUploadResult> DownloadAndUploadAsync(
        Guid channelId, string fileId, string? fileName, string? mimeType,
        CancellationToken ct)
    {
        try
        {
            var token = await botTokenProvider.GetByChannelIdAsync(channelId, ct);
            if (string.IsNullOrEmpty(token))
                return new MediaUploadFailed(fileId);

            var botClient = new TelegramBotClient(token);
            var file = await botClient.GetFile(fileId, ct);

            if (file.FileSize is > MaxFileSizeBytes)
            {
                logger.LogWarning("File {FileId} exceeds 20 MB limit ({Size} bytes), skipping download",
                    fileId, file.FileSize);
                return new MediaUploadSizeExceeded(fileId);
            }

            if (string.IsNullOrEmpty(file.FilePath))
                return new MediaUploadFailed(fileId);

            var downloadUrl = $"https://api.telegram.org/file/bot{token}/{file.FilePath}";

            using var telegramHttpClient = httpClientFactory.CreateClient();
            await using var fileStream = await telegramHttpClient.GetStreamAsync(downloadUrl, ct);
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, ct);
            memoryStream.Position = 0;

            var resolvedFileName = fileName
                                   ?? Path.GetFileName(file.FilePath)
                                   ?? $"{fileId}{GuessExtension(mimeType)}";

            var companyId = await ResolveCompanyIdAsync(channelId, ct);

            using var content = new MultipartFormDataContent();
            var fileContent = new StreamContent(memoryStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(mimeType ?? "application/octet-stream");
            content.Add(fileContent, "file", resolvedFileName);
            if (companyId != null)
                content.Add(new StringContent(companyId.Value.ToString()), "companyId");

            var fileServiceClient = httpClientFactory.CreateClient("FileServiceRest");
            var response = await fileServiceClient.PostAsync("files", content, ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("FileService upload failed with status {Status} for file {FileId}",
                    response.StatusCode, fileId);
                return new MediaUploadFailed(fileId);
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var uploadedId = root.GetProperty("id").GetGuid();
            var key = root.GetProperty("key").GetString() ?? "";

            logger.LogInformation("Media {FileId} uploaded to FileService as {UploadedId}", fileId, uploadedId);
            return new MediaUploadSuccess(uploadedId, key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download and upload media {FileId}", fileId);
            return new MediaUploadFailed(fileId);
        }
    }

    private async Task<Guid?> ResolveCompanyIdAsync(Guid channelId, CancellationToken ct)
    {
        try
        {
            var response = await botTokenServiceClient.GetCompanyIdByChannelIdAsync(
                new GetTokenByChannelIdRequest { ChannelId = channelId.ToString() },
                cancellationToken: ct);
            return Guid.TryParse(response.CompanyId, out var cid) ? cid : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to resolve companyId for channel {ChannelId}", channelId);
            return null;
        }
    }

    private static string GuessExtension(string? mimeType) => mimeType?.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/gif" => ".gif",
        "image/webp" => ".webp",
        "video/mp4" => ".mp4",
        "audio/ogg" => ".ogg",
        "audio/mpeg" => ".mp3",
        _ => ""
    };
}
