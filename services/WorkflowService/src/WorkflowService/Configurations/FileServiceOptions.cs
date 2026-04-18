namespace WorkflowService.Configurations;

public class FileServiceOptions
{
    public const string SectionName = "FileService";

    public string BaseUrl { get; set; } = "http://file-service:8080";
}
