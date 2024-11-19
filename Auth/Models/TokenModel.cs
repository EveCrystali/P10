namespace Auth.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public required string Token { get; init; }
    public required string UserId { get; init; }
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; }
}

public class RefreshRequest
{
    public required string RefreshToken { get; set; }
}