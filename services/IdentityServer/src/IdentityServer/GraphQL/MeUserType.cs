namespace IdentityServer.GraphQL;

public class MeUserType
{
    public Guid Id { get; set; }
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedAt { get; set; }
}
