using FileService.Data;
using FileService.Entities;
using FileService.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FileService.Repositories;

public class FileRepository(FileDbContext context) : IFileRepository
{
    public async Task<FileEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await context.Files.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, ct);
    }

    public async Task<FileEntity?> GetByKeyAsync(string key, CancellationToken ct = default)
    {
        return await context.Files.AsNoTracking().FirstOrDefaultAsync(f => f.Key == key, ct);
    }

    public async Task<IReadOnlyList<FileEntity>> ListAsync(Guid companyId, Guid? uploadedClientId, CancellationToken ct = default)
    {
        var query = context.Files.AsNoTracking().Where(f => f.CompanyId == companyId);
        if (uploadedClientId.HasValue)
            query = query.Where(f => f.UploadedClientId == uploadedClientId.Value);
        return await query.OrderByDescending(f => f.CreatedAt).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<FileEntity>> ListByUserAsync(Guid uploadedByUserId, Guid? uploadedClientId, CancellationToken ct = default)
    {
        var query = context.Files.AsNoTracking().Where(f => f.UploadedByUserId == uploadedByUserId);
        if (uploadedClientId.HasValue)
            query = query.Where(f => f.UploadedClientId == uploadedClientId.Value);
        return await query.OrderByDescending(f => f.CreatedAt).ToListAsync(ct);
    }

    public async Task<FileEntity> AddAsync(FileEntity entity, CancellationToken ct = default)
    {
        context.Files.Add(entity);
        await context.SaveChangesAsync(ct);
        return entity;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await context.Files.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (entity != null)
        {
            context.Files.Remove(entity);
            await context.SaveChangesAsync(ct);
        }
    }
}
