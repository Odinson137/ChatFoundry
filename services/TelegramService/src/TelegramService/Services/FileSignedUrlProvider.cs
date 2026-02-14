using File.Grpc;
using TelegramService.Interfaces;

namespace TelegramService.Services;

public sealed class FileSignedUrlProvider(File.Grpc.FileService.FileServiceClient client) : IFileSignedUrlProvider
{
    public async Task<string?> GetSignedUrlAsync(Guid fileId, CancellationToken ct = default)
    {
        try
        {
            var response = await client.GetSignedUrlAsync(
                new GetSignedUrlRequest { FileId = fileId.ToString() },
                cancellationToken: ct);
            return response?.Url;
        }
        catch
        {
            return null;
        }
    }
}
