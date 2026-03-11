using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models;

public class ForgotPasswordRequest
{
    [Required]
    public string Email { get; set; } = "";
}
