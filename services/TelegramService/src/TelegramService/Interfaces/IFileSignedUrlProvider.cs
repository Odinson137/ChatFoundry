namespace TelegramService.Interfaces;

public interface IFileSignedUrlProvider
{
    Task<ResolvedMedia?> GetSignedUrlAsync(Guid fileId, CancellationToken ct = default);
}

public record ResolvedMedia(string Url, string Extension);
