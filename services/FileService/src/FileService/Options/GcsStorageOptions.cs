namespace FileService.Options;

public class GcsStorageOptions
{
    public const string SectionName = "GcsStorage";

    public string BucketName { get; set; } = "";
    public string? CredentialFilePath { get; set; }
    public int SignedUrlValidityMinutes { get; set; } = 15;
}
