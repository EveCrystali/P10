using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
namespace Frontend.Services;

public class JwtValidationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtValidationService> _logger;

    public JwtValidationService(IConfiguration configuration, ILogger<JwtValidationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        string? secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new ArgumentNullException(secretKey, "JWT Key configuration is missing.");
        }
        try
        {
            TokenValidationParameters tokenValidationParameters = new()
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["JwtSettings:Issuer"],
                ValidAudiences = _configuration.GetSection("JwtSettings:Audience").Get<string[]>(),
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };

            JwtSecurityTokenHandler tokenHandler = new();

            // Validate the token
            ClaimsPrincipal tokenValidationResult = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken _);
            return tokenValidationResult;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogError(ex, "Token validation failed.");
            return null;
        }
    }
}