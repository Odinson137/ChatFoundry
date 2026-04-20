using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Gateway;

public class InnerJwtMiddleware
{
    public const string InnerJwtKey = "Gateway.InnerJwt";

    private readonly RequestDelegate _next;

    public InnerJwtMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
            var token = !string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader["Bearer ".Length..].Trim()
                : context.Request.Query["access_token"].FirstOrDefault();

            if (!string.IsNullOrEmpty(token))
            {
                var parts = token.Split('.');
                if (parts.Length == 5)
                {
                    
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
                                
                                SignatureValidator = (string innerToken, TokenValidationParameters _) =>
                                {
                                    innerJwtCaptured = innerToken; 
                                    return new JwtSecurityToken(innerToken);
                                }
                            };
                            handler.ValidateToken(token, validationParams, out _);
                            if (!string.IsNullOrEmpty(innerJwtCaptured))
                                context.Items[InnerJwtKey] = innerJwtCaptured;
                        }
                        catch
                        {
                            
                        }
                    }
                }
                else if (parts.Length == 3)
                {
                    
                    context.Items[InnerJwtKey] = token;
                }
            }
        }

        await _next(context);
    }
}
