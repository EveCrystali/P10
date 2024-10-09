namespace Frontend.Models;

public class AuthToken
{
    public required string Token { get; set; }
    public required string RefreshToken { get; set; }
}