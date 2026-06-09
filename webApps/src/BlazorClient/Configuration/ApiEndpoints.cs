namespace BlazorClient.Configuration;

public static class ApiEndpoints
{
    public static string Api { get; set; } = "http://localhost:5000";

    public const string OAuthScopes = "workflow client company identity file billing notification offline_access";
}