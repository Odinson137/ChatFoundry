using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models;

public class ResetPasswordRequest
{
    [Required]
    public string UserId { get; set; } = "";

    [Required]
    public string Token { get; set; } = "";

    [Required]
    [MinLength(2)]
    public string NewPassword { get; set; } = "";
}
