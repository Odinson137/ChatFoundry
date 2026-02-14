using FileService.Entities;

namespace FileService.Interfaces;

public interface IFileRepository
{
    Task<FileEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<FileEntity?> GetByKeyAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyList<FileEntity>> ListAsync(Guid companyId, Guid? uploadedClientId, CancellationToken ct = default);
    Task<IReadOnlyList<FileEntity>> ListByUserAsync(Guid uploadedByUserId, Guid? uploadedClientId, CancellationToken ct = default);
    Task<FileEntity> AddAsync(FileEntity entity, CancellationToken ct = default);
}
