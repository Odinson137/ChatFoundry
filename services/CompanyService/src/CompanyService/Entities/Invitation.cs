using CompanyService.Enums;
using Shared.Domain.Entities;

namespace CompanyService.Entities;

public class Invitation : EntityBase
{
    public Guid CompanyId { get; set; }
    public string? Email { get; set; }
    public CompanyRole Role { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public Company Company { get; set; } = null!;
}
