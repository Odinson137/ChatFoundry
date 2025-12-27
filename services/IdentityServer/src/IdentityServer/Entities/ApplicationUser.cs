using Microsoft.AspNetCore.Identity;

namespace IdentityServer.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}