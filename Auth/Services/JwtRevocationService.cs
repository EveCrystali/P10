using System;
using Auth.Data;

namespace Auth.Services;

public class JwtRevocationService(ApplicationDbContext context) : IJwtRevocationService
{
    private readonly ApplicationDbContext _context = context;

    public async Task RevokeUserTokensAsync(string userId)
    {
        List<Models.RefreshToken> refreshTokens = [.. _context.RefreshTokens.Where(rt => rt.UserId == userId)];
        foreach (Models.RefreshToken? token in refreshTokens)
        {
            token.IsRevoked = true;
        }
        await _context.SaveChangesAsync();
    }
}