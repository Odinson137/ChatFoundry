using Shared.Domain.Entities;

namespace FileService.Entities;

public class FileEntity : EntityBase
{
    public Guid? UploadedByUserId { get; set; }
    /// <summary>Отображаемое имя пользователя на момент загрузки (из JWT).</summary>
    public string? UploadedByUserName { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? UploadedClientId { get; set; }
    public string Key { get; set; } = null!;
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? Size { get; set; }
}
