using FileService.Entities;
using FileService.Interfaces;
using HotChocolate;
using Shared.Infrastructure.GraphQl;

namespace FileService.GraphQL;

public class Query(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    public async Task<IReadOnlyList<FileEntity>> GetFiles([Service] IFileRepository fileRepository, CancellationToken ct = default)
    {
        return await fileRepository.ListByUserAsync(UserId, null, ct);
    }

    public async Task<FileEntity?> GetFile(Guid id, [Service] IFileRepository fileRepository, CancellationToken ct = default)
    {
        return await fileRepository.GetByIdAsync(id, ct);
    }
}
