namespace TelegramService.Interfaces;

public interface IMediaUploader
{
    Task<MediaUploadResult> DownloadAndUploadAsync(
        Guid channelId, string fileId, string? fileName, string? mimeType,
        CancellationToken ct);
}

public abstract record MediaUploadResult;
public sealed record MediaUploadSuccess(Guid FileId, string Key) : MediaUploadResult;
public sealed record MediaUploadSizeExceeded(string TelegramFileId) : MediaUploadResult;
public sealed record MediaUploadFailed(string TelegramFileId) : MediaUploadResult;
