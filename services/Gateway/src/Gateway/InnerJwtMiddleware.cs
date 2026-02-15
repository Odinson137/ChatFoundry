using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Gateway;

/// <summary>
/// After OpenIddict Validation has authenticated the user, decrypts the JWE (if present)
/// and stores the inner signed JWT in HttpContext.Items so YARP can forward it to backends.
/// If the token is already a plain JWT (3 parts), stores it as-is.
/// </summary>
public class InnerJwtMiddleware
{
    public const string InnerJwtKey = "Gateway.InnerJwt";

    private readonly RequestDelegate _next;

    public InnerJwtMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            context.Request.Headers.Authorization.FirstOrDefault() is { } authHeader &&
            authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();
            if (!string.IsNullOrEmpty(token))
            {
                var parts = token.Split('.');
                if (parts.Length == 5)
                {
                    // JWE: decrypt and store inner JWT
                    var keyBase64 = configuration["OpenIddict:EncryptionKey"];
                    if (!string.IsNullOrEmpty(keyBase64))
                    {
                        try
                        {
                            var key = new SymmetricSecurityKey(Convert.FromBase64String(keyBase64));
                            var handler = new JwtSecurityTokenHandler();
                            string? innerJwtCaptured = null;
                            var validationParams = new TokenValidationParameters
                            {
                                TokenDecryptionKey = key,
                                ValidateIssuerSigningKey = false,
                                ValidateIssuer = false,
                                ValidateAudience = false,
                                ValidateLifetime = false,
                                // Токен уже проверен OpenIddict Validation; здесь только расшифровка, подпись не проверяем.
                                SignatureValidator = (string innerToken, TokenValidationParameters _) =>
                                {
                                    innerJwtCaptured = innerToken; // сохраняем исходную строку с подписью
                                    return new JwtSecurityToken(innerToken);
                                }
                            };
                            handler.ValidateToken(token, validationParams, out _);
                            if (!string.IsNullOrEmpty(innerJwtCaptured))
                                context.Items[InnerJwtKey] = innerJwtCaptured;
                        }
                        catch
                        {
                            // Decryption failed; do not set InnerJwt, transform may forward original
                        }
                    }
                }
                else if (parts.Length == 3)
                {
                    // Already a signed JWT (encryption disabled)
                    context.Items[InnerJwtKey] = token;
                }
            }
        }

        await _next(context);
    }
}
