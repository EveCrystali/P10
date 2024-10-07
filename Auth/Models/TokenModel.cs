using System;

namespace Auth.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; }
    public string UserId { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; }
}

public class RefreshRequest
{
    public string RefreshToken { get; set; }
}

public class RevokeTokensRequest
{
    public string UserId { get; set; }
}
