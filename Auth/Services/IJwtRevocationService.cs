using System;

namespace Auth.Services;

public interface IJwtRevocationService
{
    Task RevokeUserTokensAsync(string userId);
}