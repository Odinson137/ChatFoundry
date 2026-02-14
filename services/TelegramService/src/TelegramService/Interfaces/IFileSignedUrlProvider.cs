namespace TelegramService.Interfaces;

public interface IFileSignedUrlProvider
{
    Task<string?> GetSignedUrlAsync(Guid fileId, CancellationToken ct = default);
}
