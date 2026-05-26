using System.Text.Json;
using BillingService.Data;
using BillingService.Enums;
using BillingService.Options;
using BillingService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BillingService.Controllers;

[ApiController]
[Route("webhook")]
public class HeleketWebhookController(
    BillingDbContext db,
    BillingAccountService billingAccount,
    IOptions<HeleketOptions> options,
    ILogger<HeleketWebhookController> logger) : ControllerBase
{
    private readonly HeleketOptions _opt = options.Value;

    [HttpPost("heleket")]
    [AllowAnonymous]
    public async Task<IActionResult> Heleket(CancellationToken ct)
    {
        Request.EnableBuffering();
        string raw;
        using (var reader = new StreamReader(Request.Body, leaveOpen: true))
        {
            raw = await reader.ReadToEndAsync(ct);
        }

        if (string.IsNullOrWhiteSpace(raw))
            return BadRequest();

        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        if (!root.TryGetProperty("sign", out var signEl))
            return BadRequest("missing sign");

        var sign = signEl.GetString() ?? "";

        if (!_opt.SkipWebhookSignatureVerification)
        {
            if (!HeleketSignature.VerifyWebhookJson(raw, sign, _opt.PaymentApiKey))
            {
                logger.LogWarning("Invalid Heleket webhook signature");
                return Unauthorized();
            }
        }

        var status = root.TryGetProperty("status", out var st) ? st.GetString() : null;
        var orderId = root.TryGetProperty("order_id", out var oid) ? oid.GetString() : null;

        if (string.IsNullOrEmpty(orderId))
            return BadRequest();

        var payment = await db.Payments.FirstOrDefaultAsync(p => p.OrderId == orderId, ct);
        if (payment is null)
        {
            logger.LogWarning("Payment not found for order_id {OrderId}", orderId);
            return Ok();
        }

        if (payment.Status == PaymentStatus.Paid)
            return Ok();

        payment.WebhookPayload = raw;
        payment.ModifiedAt = DateTime.UtcNow;

        if (status is "paid" or "paid_over")
        {
            var credit = payment.Amount;
            if (root.TryGetProperty("merchant_amount", out var ma) && ma.ValueKind == JsonValueKind.String)
            {
                if (decimal.TryParse(ma.GetString(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                    credit = parsed;
            }

            payment.Status = PaymentStatus.Paid;
            await db.SaveChangesAsync(ct);

            await billingAccount.CreditBalanceFromPaymentAsync(
                payment.CompanyId,
                credit,
                $"Heleket top-up order {orderId}",
                payment.Id,
                ct);

            return Ok();
        }

        if (status is "cancel" or "fail" or "wrong_amount")
        {
            payment.Status = PaymentStatus.Cancelled;
            await db.SaveChangesAsync(ct);
        }

        return Ok();
    }
}
