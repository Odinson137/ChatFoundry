using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BillingService.Services;

public static class HeleketSignature
{
    public static string SignJsonBody(string jsonBody, string apiKey)
    {
        var b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(jsonBody));
        var input = b64 + apiKey;
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>
    /// Verifies webhook signature (Heleket: MD5(base64(json_without_sign) + apiKey) == sign).
    /// </summary>
    public static bool VerifyWebhookJson(string jsonBody, string signFromPayload, string apiKey)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonBody);
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
                return false;

            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
            {
                WriteObjectWithoutSign(doc.RootElement, writer);
            }

            var payloadWithoutSign = Encoding.UTF8.GetString(stream.ToArray());
            var expected = SignJsonBody(payloadWithoutSign, apiKey);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(signFromPayload));
        }
        catch
        {
            return false;
        }
    }

    private static void WriteObjectWithoutSign(JsonElement el, Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        foreach (var prop in el.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            if (prop.Name.Equals("sign", StringComparison.OrdinalIgnoreCase))
                continue;
            prop.WriteTo(writer);
        }
        writer.WriteEndObject();
    }
}

