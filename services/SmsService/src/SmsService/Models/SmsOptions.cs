namespace SmsService.Models;

public class SmsOptions
{
    public const string SectionName = "Sms";

    public string ApiUrl { get; set; } = "https://api.infinireach.io/api/v1/messages";
}
