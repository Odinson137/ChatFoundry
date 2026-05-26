using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BillingService.Data;
using BillingService.Entities;
using BillingService.Enums;
using BillingService.Options;
using Microsoft.Extensions.Options;

namespace BillingService.Services;

public class HeleketPaymentService(
    BillingDbContext db,
    IHttpClientFactory httpClientFactory,
    IOptions<HeleketOptions> options,
    ILogger<HeleketPaymentService> logger)
{
    private readonly HeleketOptions _opt = options.Value;

    public async Task<HeleketCreateInvoiceResult?> CreateTopUpInvoiceAsync(
        Guid companyId,
        decimal amount,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_opt.MerchantId) || string.IsNullOrWhiteSpace(_opt.PaymentApiKey))
        {
            logger.LogWarning("Heleket merchant or API key not configured");
            return null;
        }

        var payment = new Payment
        {
            CompanyId = companyId,
            OrderId = "", // set after we know Id
            Amount = amount,
            Currency = "USDT",
            Status = PaymentStatus.Pending
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync(ct);

        payment.OrderId = payment.Id.ToString("N");
        payment.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        var callbackUrl = $"{_opt.PublicGatewayBaseUrl.TrimEnd('/')}/billing/webhook/heleket";
        var bodyObj = new Dictionary<string, object?>
        {
            ["amount"] = amount.ToString("0.########", System.Globalization.CultureInfo.InvariantCulture),
            ["currency"] = "USDT",
            ["order_id"] = payment.OrderId,
            ["network"] = "tron",
            ["url_callback"] = callbackUrl,
            ["lifetime"] = 3600
        };

        var json = JsonSerializer.Serialize(bodyObj, new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });

        var sign = HeleketSignature.SignJsonBody(json, _opt.PaymentApiKey);

        var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.TryAddWithoutValidation("merchant", _opt.MerchantId);
        client.DefaultRequestHeaders.TryAddWithoutValidation("sign", sign);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var url = new Uri(new Uri(_opt.ApiBaseUrl), "v1/payment");
        var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"), ct);
        var responseText = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Heleket API error {Status}: {Body}", response.StatusCode, responseText);
            return null;
        }

        using var doc = JsonDocument.Parse(responseText);
        var root = doc.RootElement;
        if (root.GetProperty("state").GetInt32() != 0)
        {
            logger.LogError("Heleket returned error state: {Body}", responseText);
            return null;
        }

        var result = root.GetProperty("result");
        var uuid = result.GetProperty("uuid").GetString() ?? "";
        var payUrl = result.TryGetProperty("url", out var urlEl) ? urlEl.GetString() : null;

        payment.HeleketUuid = uuid;
        payment.ModifiedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        return new HeleketCreateInvoiceResult(payUrl ?? "", payment.OrderId);
    }
}

public record HeleketCreateInvoiceResult(string PaymentUrl, string OrderId);
