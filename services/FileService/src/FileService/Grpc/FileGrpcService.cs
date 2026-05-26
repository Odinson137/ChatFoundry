using File.Grpc;
using FileService.Interfaces;
using FileService.Services;
using Grpc.Core;

namespace FileService.Grpc;

public sealed class FileGrpcService(
    IFileRepository fileRepository,
    IStorageService storage)
    : File.Grpc.FileService.FileServiceBase
{
    public override async Task<GetSignedUrlResponse> GetSignedUrl(
        GetSignedUrlRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.FileId) || !Guid.TryParse(request.FileId, out var fileId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid file_id"));

        var file = await fileRepository.GetByIdAsync(fileId, context.CancellationToken);
        if (file == null)
            throw new RpcException(new Status(StatusCode.NotFound, "File not found"));

        var url = await storage.GetSignedUrlAsync(file.Key, ct: context.CancellationToken);
        if (string.IsNullOrEmpty(url))
            throw new RpcException(new Status(StatusCode.Unavailable, "Signed URL could not be generated"));

        var extension = string.IsNullOrEmpty(file.OriginalFileName)
            ? ""
            : Path.GetExtension(file.OriginalFileName).ToLowerInvariant();
        return new GetSignedUrlResponse { Url = url, Extension = extension };
    }
}
