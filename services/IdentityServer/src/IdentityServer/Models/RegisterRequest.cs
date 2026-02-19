namespace IdentityServer.Models;

public class RegisterRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public string? InviteToken { get; set; }
    public string? CompanyName { get; set; }
}
