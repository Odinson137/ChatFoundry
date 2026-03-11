using System.ComponentModel.DataAnnotations;

namespace IdentityServer.Models;

public class ResendConfirmationRequest
{
    [Required] public string Email { get; set; } = "";
}
