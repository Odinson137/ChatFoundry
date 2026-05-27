namespace WorkflowService.Configurations;

public class FreeLlmOptions
{
    public const string SectionName = "FreeLlm";

    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string Model { get; set; } = "auto";
}
