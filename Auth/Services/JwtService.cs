using System;
using Auth.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Auth.Services;

public class JwtService(IConfiguration configuration) : IJwtService
{
    private readonly IConfiguration _configuration = configuration;

    /// <summary>
    /// Generates a JSON Web Token (JWT) for the given user ID, username, and roles.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="userName">The username of the user.</param>
    /// <param name="roles">The roles assigned to the user.</param>
    /// <returns>The generated JWT.</returns>
    public string GenerateToken(string userId, string userName, string[] roles)
    {
        // Create a list of claims for the token. Each claim represents a piece of information about the user.
        List<Claim> claims =
        [
            // Add the user ID as a claim
            new Claim(ClaimTypes.NameIdentifier, userId),
            // Add the username as a claim
            new Claim(ClaimTypes.Name, userName),
            // Add each role as a claim
            .. roles.Select(role => new Claim(ClaimTypes.Role, role)),
        ];

        // Get the secret key from the environment variables. This key is used to sign the token.
        string? secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");

        // If the secret key is not defined, throw an exception.
        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("The JWT secret key is not defined in the environment variables");
        }

        // Convert the secret key to a 256-bit key
        byte[] keyBytes = new byte[32]; 
        byte[] secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
        Array.Copy(secretKeyBytes, keyBytes, Math.Min(secretKeyBytes.Length, keyBytes.Length));

        // Create the signing credentials using the secret key.
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(secretKey));
        SigningCredentials creds = new(key, SecurityAlgorithms.HmacSha256);

        // Create the JWT token with the issuer, audience, claims, expiration time, and signing credentials.
        JwtSecurityToken token = new(
            // The issuer is the entity that issued the token
            issuer: _configuration["Jwt:Issuer"],
            // The audience is the entity that the token is intended for
            audience: _configuration["Jwt:Audience"],
            // The claims are the information about the user
            claims: claims,
            // The token expires after 1 hour
            expires: DateTime.UtcNow.AddMinutes(double.Parse(_configuration["Jwt:TokenLifetimeMinutes"])),
            // The signing credentials are used to sign the token
            signingCredentials: creds);

        // Create a JwtSecurityTokenHandler and use it to write the token as a string.
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a refresh token for the provided user ID.
    /// </summary>
    /// <param name="userId">The ID of the user.</param>
    /// <returns>The generated refresh token.</returns>
    /// <remarks>
    /// This method creates a new instance of the <see cref="RefreshToken"/> class and sets its properties
    /// to the values generated or provided. It generates a unique token using <see cref="Guid.NewGuid"/>
    /// and converts it to a string using <see cref="Guid.ToString"/>. It sets the <see cref="RefreshToken.UserId"/>
    /// property to the provided user ID. It sets the <see cref="RefreshToken.ExpiryDate"/> property to the current
    /// date and time plus the number of days specified in the configuration for Jwt:RefreshTokenLifetimeDays.
    /// It sets the <see cref="RefreshToken.IsRevoked"/> property to false.
    /// </remarks>
    public RefreshToken GenerateRefreshToken(string userId)
    {
        // Create a new instance of the RefreshToken class
        RefreshToken refreshToken = new()
        {
            // Generate a unique token using Guid.NewGuid and convert it to a string
            Token = Guid.NewGuid().ToString(),
            UserId = userId,
            // Set the ExpiryDate property to the current date and time plus the number of days specified in the configuration for Jwt:RefreshTokenLifetimeDays
            ExpiryDate = DateTime.UtcNow.AddDays(int.Parse(_configuration["Jwt:RefreshTokenLifetimeDays"])),
            IsRevoked = false
        };
        return refreshToken;
    }
}