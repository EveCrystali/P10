using Auth.Models;

namespace Auth.Services;

public interface IJwtService
{
    string GenerateToken(string userId, string userName, string[] roles);

    RefreshToken GenerateRefreshToken(string userId);
}