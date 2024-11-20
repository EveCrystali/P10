using Auth.Data;
using Auth.Models;
namespace Auth.Services;

public class JwtRevocationService(ApplicationDbContext context) : IJwtRevocationService
{

    public async Task RevokeUserTokensAsync(string userId)
    {
        List<RefreshToken> refreshTokens = [.. context.RefreshTokens.Where(rt => rt.UserId == userId)];
        foreach (RefreshToken? token in refreshTokens)
        {
            token.IsRevoked = true;
        }
        await context.SaveChangesAsync();
    }
}