using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
using FileService.Options;
using Microsoft.Extensions.Options;

namespace FileService.Services;

public interface IStorageService
{
    Task<(string Key, string Url)> UploadAsync(Stream content, string fileName, string? contentType, CancellationToken ct = default);
    Task<string?> GetSignedUrlAsync(string key, TimeSpan? validity = null, string? downloadFileName = null, CancellationToken ct = default);
    IAsyncEnumerable<FileEntry> ListAsync(string? prefix, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
}

public record FileEntry(string Key, string? Name, string? Url);

public class GcsStorageService : IStorageService
{
    private const string UploadsPrefix = "uploads/";

    private readonly StorageClient _client;
    private readonly UrlSigner? _urlSigner;
    private readonly string _bucketName;
    private readonly TimeSpan _signedUrlValidity;

    private static string ToObjectName(string key)
    {
        var k = key.TrimStart('/');
        return k.StartsWith(UploadsPrefix, StringComparison.Ordinal) ? k : UploadsPrefix + k;
    }

    private static string ToShortKey(string objectName) =>
        objectName.StartsWith(UploadsPrefix, StringComparison.Ordinal)
            ? objectName.Substring(UploadsPrefix.Length)
            : objectName;

    public GcsStorageService(IOptions<GcsStorageOptions> options)
    {
        var opts = options.Value;
        _bucketName = opts.BucketName ?? throw new InvalidOperationException("GcsStorage:BucketName is required");
        _signedUrlValidity = TimeSpan.FromMinutes(opts.SignedUrlValidityMinutes is > 0 ? opts.SignedUrlValidityMinutes : 15);

        if (!string.IsNullOrEmpty(opts.CredentialFilePath))
        {
            var credential = GoogleCredential.FromFile(opts.CredentialFilePath);
            _client = StorageClient.Create(credential);
            _urlSigner = UrlSigner.FromCredential(credential);
        }
        else
        {
            _client = StorageClient.Create();
            _urlSigner = null;
        }
    }

    public async Task<(string Key, string Url)> UploadAsync(Stream content, string fileName, string? contentType, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName);
        var key = $"{Guid.NewGuid():N}{ext}";
        var objectName = ToObjectName(key);
        var contentTypeToUse = contentType ?? "application/octet-stream";

        await _client.UploadObjectAsync(
            _bucketName,
            objectName,
            contentTypeToUse,
            content,
            cancellationToken: ct);

        var url = await GetSignedUrlAsync(key, ct: ct) ?? key;
        return (key, url);
    }

    public async Task<string?> GetSignedUrlAsync(string key, TimeSpan? validity = null, string? downloadFileName = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (_urlSigner == null) return null;
        var objectName = ToObjectName(key);
        var duration = validity ?? _signedUrlValidity;

        if (!string.IsNullOrEmpty(downloadFileName))
        {
            var template = UrlSigner.RequestTemplate
                .FromBucket(_bucketName)
                .WithObjectName(objectName)
                .WithHttpMethod(HttpMethod.Get)
                .WithQueryParameters(new Dictionary<string, IEnumerable<string>>
                {
                    ["response-content-disposition"] = [$"attachment; filename=\"{downloadFileName}\""]
                });
            var options = UrlSigner.Options.FromDuration(duration);
            return await _urlSigner.SignAsync(template, options, ct);
        }

        return await _urlSigner.SignAsync(_bucketName, objectName, duration, HttpMethod.Get, cancellationToken: ct);
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(key)) return;
        var objectName = ToObjectName(key);
        await _client.DeleteObjectAsync(_bucketName, objectName, cancellationToken: ct);
    }

    public async IAsyncEnumerable<FileEntry> ListAsync(string? prefix, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var listPrefix = string.IsNullOrEmpty(prefix) ? UploadsPrefix : ToObjectName(prefix);
        var list = _client.ListObjectsAsync(_bucketName, listPrefix);
        await foreach (var obj in list.WithCancellation(ct))
        {
            var shortKey = ToShortKey(obj.Name);
            var url = await GetSignedUrlAsync(shortKey, ct: ct);
            yield return new FileEntry(shortKey, Path.GetFileName(obj.Name), url);
        }
    }
}
