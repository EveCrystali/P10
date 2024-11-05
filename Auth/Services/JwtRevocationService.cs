using Auth.Data;
using Auth.Models;
namespace Auth.Services;

public class JwtRevocationService(ApplicationDbContext context) : IJwtRevocationService
{
    private readonly ApplicationDbContext _context = context;

    public async Task RevokeUserTokensAsync(string userId)
    {
        List<RefreshToken> refreshTokens = [.. _context.RefreshTokens.Where(rt => rt.UserId == userId)];
        foreach (RefreshToken? token in refreshTokens)
        {
            token.IsRevoked = true;
        }
        await _context.SaveChangesAsync();
    }
}