using File.Grpc;
using TelegramService.Interfaces;

namespace TelegramService.Services;

public sealed class FileSignedUrlProvider(File.Grpc.FileService.FileServiceClient client) : IFileSignedUrlProvider
{
    public async Task<ResolvedMedia?> GetSignedUrlAsync(Guid fileId, CancellationToken ct = default)
    {
        try
        {
            var response = await client.GetSignedUrlAsync(
                new GetSignedUrlRequest { FileId = fileId.ToString() },
                cancellationToken: ct);
            if (response == null || string.IsNullOrEmpty(response.Url))
                return null;
            var ext = response.Extension ?? "";
            return new ResolvedMedia(response.Url, ext);
        }
        catch
        {
            return null;
        }
    }
}
