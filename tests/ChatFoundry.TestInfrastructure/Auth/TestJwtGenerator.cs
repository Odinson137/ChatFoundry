namespace ChatFoundry.TestInfrastructure.Auth;

public static class TestJwtGenerator
{
    public static string GenerateToken(Guid userId, Guid? companyId = null, string[]? scopes = null)
    {
        var compIdStr = companyId?.ToString() ?? Guid.Empty.ToString();
        var scopesStr = scopes != null ? string.Join(",", scopes) : "";
        return $"{userId}:{compIdStr}:{scopesStr}";
    }
}
