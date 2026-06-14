using FileService.Entities;
using FileService.Interfaces;
using HotChocolate;
using Shared.Infrastructure.GraphQl;

namespace FileService.GraphQL;

public class Query(IHttpContextAccessor httpContextAccessor) : BaseGraphQl(httpContextAccessor)
{
    public async Task<IReadOnlyList<FileEntity>> GetFiles([Service] IFileRepository fileRepository, CancellationToken ct = default)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("Company ID is required.");

        return await fileRepository.ListAsync(CompanyId.Value, null, ct);
    }

    public async Task<FileEntity?> GetFile(Guid id, [Service] IFileRepository fileRepository, CancellationToken ct = default)
    {
        if (!CompanyId.HasValue)
            throw new GraphQLException("Company ID is required.");

        var file = await fileRepository.GetByIdAsync(id, ct);
        if (file == null || file.CompanyId != CompanyId.Value)
            return null;

        return file;
    }
}
