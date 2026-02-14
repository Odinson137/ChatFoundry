using FileService.Options;
using Microsoft.Extensions.Options;

namespace FileService.Services;

public interface IFileUrlBuilder
{
    string BuildUrl(string key);
}

public class FileUrlBuilder(IOptions<GcsStorageOptions> options) : IFileUrlBuilder
{
    public string BuildUrl(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return "";
        var opts = options.Value;
        var path = key.TrimStart('/').StartsWith("uploads/", StringComparison.Ordinal) ? key.TrimStart('/') : "uploads/" + key.TrimStart('/');
        return $"https://storage.googleapis.com/{opts.BucketName}/{path}";
    }
}
