using CompanyService.Enums;
using Shared.Domain.Entities;

namespace CompanyService.Entities;

public class CompanyMember : EntityBase
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public CompanyRole Role { get; set; }
    public bool IsActive { get; set; }

    public Company Company { get; set; } = null!;
}
