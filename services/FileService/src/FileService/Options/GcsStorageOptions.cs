namespace FileService.Options;

public class GcsStorageOptions
{
    public const string SectionName = "GcsStorage";

    public string BucketName { get; set; } = "";
    public string? CredentialFilePath { get; set; }
    /// <summary>Validity of signed URLs in minutes. Default 15.</summary>
    public int SignedUrlValidityMinutes { get; set; } = 15;
}
