using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Auth.Models;
using Microsoft.IdentityModel.Tokens;
namespace Auth.Services;

public class JwtService(IConfiguration configuration) : IJwtService
{

    /// <summary>
    ///     Generates a JSON Web Token (JWT) for the given user ID, username, and roles.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="userName">The username of the user.</param>
    /// <param name="roles">The roles assigned to the user.</param>
    /// <returns>The generated JWT.</returns>
    public string GenerateToken(string userId, string userName, string[] roles)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userName)
        ];

        // Ajouter les rôles en tant que claims
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        // Ajouter toutes les audiences en tant que claims 'aud'
        string[] audiences = configuration.GetSection("JwtSettings:Audience").Get<string[]>() ?? [];

        foreach (string audience in audiences)
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Aud, audience));
        }

        string? secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("The JWT secret key is not defined in the environment variables");
        }

        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(secretKey));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        DateTime expiryTimeFromConfirguration = DateTime.UtcNow.AddMinutes(int.TryParse(configuration["JwtSettings:TokenLifetimeMinutes"], out int lifeTimeMinutes) ? lifeTimeMinutes : 10);

        JwtSecurityToken token = new(
                                     configuration["JwtSettings:Issuer"],
                                     // Ne pas passer l'audience ici car elle est ajoutée en tant que claims
                                     claims: claims,
                                     expires: expiryTimeFromConfirguration,
                                     signingCredentials: creds
                                    );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    ///     Generates a refresh token for the provided user ID.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The generated refresh token.</returns>
    /// <remarks>
    ///     This method creates a new instance of the <see cref="RefreshToken" /> class and sets its properties
    ///     to the values generated or provided. It generates a unique token using <see cref="Guid.NewGuid" />
    ///     and converts it to a string using <see cref="Guid.ToString()" />. It sets the <see cref="RefreshToken.UserId" />
    ///     property to the provided user ID. It sets the <see cref="RefreshToken.ExpiryDate" /> property to the current
    ///     date and time plus the number of days specified in the configuration for Jwt:RefreshTokenLifetimeDays.
    ///     It sets the <see cref="RefreshToken.IsRevoked" /> property to false.
    /// </remarks>
    public RefreshToken GenerateRefreshToken(string userId)
    {
        DateTime expiryDateFromConfirguration = DateTime.UtcNow.AddDays(int.TryParse(configuration["JwtSettings:RefreshTokenLifetimeDays"], out int lifetimeDays) ? lifetimeDays : 30);

        // Create a new instance of the RefreshToken class
        RefreshToken refreshToken = new()
        {
            // Generate a unique token using Guid.NewGuid and convert it to a string
            Token = Guid.NewGuid().ToString(),
            UserId = userId,
            // Set the ExpiryDate property to the current date and time plus the number of days specified in the configuration for Jwt:RefreshTokenLifetimeDays
            ExpiryDate = expiryDateFromConfirguration,
            IsRevoked = false
        };
        return refreshToken;
    }
}