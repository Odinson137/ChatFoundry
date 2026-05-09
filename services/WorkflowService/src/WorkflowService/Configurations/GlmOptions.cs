namespace WorkflowService.Configurations;

public class GlmOptions
{
    public const string SectionName = "Glm";

    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string Model { get; set; } = "GLM-4.6";
}
