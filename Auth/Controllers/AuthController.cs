using System.Security.Claims;
using Auth.Data;
using Auth.Models;
using Auth.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Auth.Controllers;

[Route("auth")]
[ApiController]
public class AuthController(
    UserManager<User> userManager,
    IJwtService jwtService,
    ILogger<AuthController> logger, ApplicationDbContext context) : ControllerBase
{
    private readonly ApplicationDbContext _context = context;
    private readonly IJwtService _jwtService = jwtService;
    private readonly ILogger<AuthController> _logger = logger;
    private readonly UserManager<User> _userManager = userManager;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        const string messageLog = "Login request in Auth microservice from Auth controller in Login method";
        _logger.LogInformation(messageLog);
        User? user = await _userManager.FindByNameAsync(model.Username);
        switch (user)
        {
            case { UserName: not null } when await _userManager.CheckPasswordAsync(user, model.Password):
                {
                    user.LastLoginDate = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);

                    _logger.LogInformation("User found, last login date updated. Generating JWT token and refresh token.");
                    IList<string> userRoles = await _userManager.GetRolesAsync(user);
                    string token = _jwtService.GenerateToken(user.Id, user.UserName, userRoles.ToArray());
                    RefreshToken refreshToken = _jwtService.GenerateRefreshToken(user.Id);

                    await _context.RefreshTokens.AddAsync(refreshToken);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Login successful, returning tokens.");

                    return Ok(new
                    {
                        Token = token,
                        RefreshToken = refreshToken.Token
                    });
                }
            case null:
                _logger.LogError("User not found");
                return NotFound("User not found");
        }
        if (!await _userManager.CheckPasswordAsync(user, model.Password))
        {
            _logger.LogError("User found but password is incorrect");
            return Unauthorized("Invalid username or password");
        }
        _logger.LogError("Something went wrong");
        return StatusCode(500, "Something went wrong");
    }

    [HttpPost]
    [Authorize]
    [Route("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!ModelState.IsValid)
        {
            _logger.LogError("Model state is not valid.");
            return BadRequest("Model state is not valid.");
        }

        _logger.LogInformation("Logout request received.");
        // Get User ID from JWT token
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("Impossible to get user ID.");
        }

        // Revoke all refresh tokens for the user
        bool isRevoked = await RevokeAllTokensForUserAsync(userId);
        if (!isRevoked)
        {
            _logger.LogWarning("No tokens found or an error occurred during revocation.");
        }

        return Ok(new
        {
            Message = "Logout successful"
        });
    }

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        User user = new()
        {
            UserName = model.Username,
            Email = model.Email,
            PasswordHash = model.Password
        };
        IdentityResult result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            return Ok(new
            {
                Message = "User registered successfully"
            });
        }
        return BadRequest(result.Errors);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest model)
    {
        _logger.LogInformation("Refresh token request received.");
        
        RefreshToken? refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == model.RefreshToken);
        if (refreshToken == null || refreshToken.ExpiryDate < DateTime.UtcNow || refreshToken.IsRevoked)
        {
            _logger.LogDebug("Invalid or expired refresh token.");
            return Unauthorized();
        }

        User? user = await _userManager.FindByIdAsync(refreshToken.UserId);
        if (user == null || !user.IsUserActive() || user.UserName == null)
        {
            _logger.LogDebug("User not found or inactive.");
            return Unauthorized();
        }

        IList<string> userRoles = await _userManager.GetRolesAsync(user);
        if (user.UserName != null)
        {
            _logger.LogDebug("Generating new tokens for user.");
            string newToken = _jwtService.GenerateToken(user.Id, user.UserName, userRoles.ToArray());
            RefreshToken newRefreshToken = _jwtService.GenerateRefreshToken(user.Id);

            _logger.LogDebug("Revoking previous refresh token.");
            refreshToken.IsRevoked = true;
            _context.RefreshTokens.Update(refreshToken);

            _logger.LogDebug("Adding new refresh token.");
            _context.RefreshTokens.Add(newRefreshToken);

            _logger.LogDebug("Removing old refresh tokens.");
            List<RefreshToken> oldTokens = await _context.RefreshTokens
                                                         .Where(rt => rt.UserId == user.Id && (rt.ExpiryDate < DateTime.UtcNow || rt.IsRevoked))
                                                         .ToListAsync();
            _context.RefreshTokens.RemoveRange(oldTokens);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Tokens refreshed successfully.");
            return Ok(new
            {
                Token = newToken,
                RefreshToken = newRefreshToken.Token
            });
        }
        else
        {
            _logger.LogWarning("Authorization failed.");
            return Unauthorized();
        }
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeTokens(string userId)
    {
        User? user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        bool isRevoked = await RevokeAllTokensForUserAsync(userId);
        if (!isRevoked)
        {
            return BadRequest("No tokens found or an error occurred during revocation.");
        }
        return Ok("All tokens of the user have been revoked successfully.");
    }

    private async Task<bool> RevokeAllTokensForUserAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("User ID is null or empty, cannot revoke tokens.");
            return false;
        }

        // Find all refresh tokens associated with the user
        List<RefreshToken> userTokens = await _context.RefreshTokens
                                                      .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                                                      .ToListAsync();

        if (userTokens.Count == 0)
        {
            _logger.LogWarning("No active refresh tokens found for the user.");
            return false;
        }

        // Revoke all refresh tokens
        foreach (RefreshToken? token in userTokens)
        {
            token.IsRevoked = true;
        }

        _context.RefreshTokens.UpdateRange(userTokens);
        await _context.SaveChangesAsync();
        _logger.LogInformation("All refresh tokens for user have been revoked.");

        return true;
    }
}