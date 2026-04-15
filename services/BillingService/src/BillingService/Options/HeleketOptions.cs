namespace BillingService.Options;

public class HeleketOptions
{
    public const string SectionName = "Heleket";

    public string ApiBaseUrl { get; set; } = "https://api.heleket.com/";
    public string MerchantId { get; set; } = "";
    public string PaymentApiKey { get; set; } = "";
    /// <summary>Public URL prefix for webhook, e.g. https://gateway:8443/billing</summary>
    public string PublicGatewayBaseUrl { get; set; } = "https://localhost:5000";
    public bool SkipWebhookSignatureVerification { get; set; }
}
