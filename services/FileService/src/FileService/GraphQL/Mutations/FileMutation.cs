using FileService.Entities;
using FileService.Interfaces;
using FileService.Services;
using HotChocolate;
using HotChocolate.Types;
using Shared.Infrastructure.GraphQl;

namespace FileService.GraphQL.Mutations;

[ExtendObjectType(typeof(Mutation))]
public class FileMutation(
    IHttpContextAccessor httpContextAccessor,
    IStorageService storage,
    IFileRepository fileRepository) : BaseGraphQl(httpContextAccessor)
{
    public async Task<FileEntity> UploadFile(
        Guid companyId,
        Guid? uploadedClientId,
        IFile file,
        CancellationToken ct = default)
    {
        var fileName = file.Name ?? "file";
        var contentType = file.ContentType;

        await using var stream = file.OpenReadStream();
        var (key, _) = await storage.UploadAsync(stream, fileName, contentType, ct);

        var size = file.Length;
        var principal = HttpContextAccessor.HttpContext?.User;
        var entity = new FileEntity
        {
            UploadedByUserId = UserId != Guid.Empty ? UserId : null,
            UploadedByUserName = UploaderDisplayNameHelper.FromPrincipal(principal),
            CompanyId = companyId,
            UploadedClientId = uploadedClientId,
            Key = key,
            OriginalFileName = fileName,
            ContentType = contentType,
            Size = size > 0 ? size : null
        };

        await fileRepository.AddAsync(entity, ct);
        return entity;
    }
}
