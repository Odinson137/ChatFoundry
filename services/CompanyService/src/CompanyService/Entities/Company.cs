using Shared.Domain.Entities;

namespace CompanyService.Entities;

public class Company : EntityBase
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int MaxUsers { get; set; }

    public ICollection<CompanyMember> Members { get; set; } = [];
}
