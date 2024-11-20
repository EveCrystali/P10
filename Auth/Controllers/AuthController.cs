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

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        const string messageLog = "Login request in Auth microservice from Auth controller in Login method";
        logger.LogInformation(messageLog);
        User? user = await userManager.FindByNameAsync(model.Username);
        switch (user)
        {
            case { UserName: not null } when await userManager.CheckPasswordAsync(user, model.Password):
                {
                    user.LastLoginDate = DateTime.UtcNow;
                    await userManager.UpdateAsync(user);

                    logger.LogInformation("User found, last login date updated. Generating JWT token and refresh token.");
                    IList<string> userRoles = await userManager.GetRolesAsync(user);
                    string token = jwtService.GenerateToken(user.Id, user.UserName, userRoles.ToArray());
                    RefreshToken refreshToken = jwtService.GenerateRefreshToken(user.Id);

                    await context.RefreshTokens.AddAsync(refreshToken);
                    await context.SaveChangesAsync();

                    logger.LogInformation("Login successful, returning tokens.");

                    return Ok(new
                    {
                        Token = token,
                        RefreshToken = refreshToken.Token
                    });
                }
            case null:
                logger.LogError("User not found");
                return NotFound("User not found");
        }
        if (!await userManager.CheckPasswordAsync(user, model.Password))
        {
            logger.LogError("User found but password is incorrect");
            return Unauthorized("Invalid username or password");
        }
        logger.LogError("Something went wrong");
        return StatusCode(500, "Something went wrong");
    }

    [HttpPost]
    [Authorize]
    [Route("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!ModelState.IsValid)
        {
            logger.LogError("Model state is not valid.");
            return BadRequest("Model state is not valid.");
        }

        logger.LogInformation("Logout request received.");
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
            logger.LogWarning("No tokens found or an error occurred during revocation.");
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
        IdentityResult result = await userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "User");
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
        logger.LogInformation("Refresh token request received.");
        
        RefreshToken? refreshToken = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == model.RefreshToken);
        if (refreshToken == null || refreshToken.ExpiryDate < DateTime.UtcNow || refreshToken.IsRevoked)
        {
            logger.LogDebug("Invalid or expired refresh token.");
            return Unauthorized();
        }

        User? user = await userManager.FindByIdAsync(refreshToken.UserId);
        if (user == null || !user.IsUserActive() || user.UserName == null)
        {
            logger.LogDebug("User not found or inactive.");
            return Unauthorized();
        }

        IList<string> userRoles = await userManager.GetRolesAsync(user);
        if (user.UserName != null)
        {
            logger.LogDebug("Generating new tokens for user.");
            string newToken = jwtService.GenerateToken(user.Id, user.UserName, userRoles.ToArray());
            RefreshToken newRefreshToken = jwtService.GenerateRefreshToken(user.Id);

            logger.LogDebug("Revoking previous refresh token.");
            refreshToken.IsRevoked = true;
            context.RefreshTokens.Update(refreshToken);

            logger.LogDebug("Adding new refresh token.");
            context.RefreshTokens.Add(newRefreshToken);

            logger.LogDebug("Removing old refresh tokens.");
            List<RefreshToken> oldTokens = await context.RefreshTokens
                                                         .Where(rt => rt.UserId == user.Id && (rt.ExpiryDate < DateTime.UtcNow || rt.IsRevoked))
                                                         .ToListAsync();
            context.RefreshTokens.RemoveRange(oldTokens);

            await context.SaveChangesAsync();

            logger.LogInformation("Tokens refreshed successfully.");
            return Ok(new
            {
                Token = newToken,
                RefreshToken = newRefreshToken.Token
            });
        }
        else
        {
            logger.LogWarning("Authorization failed.");
            return Unauthorized();
        }
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeTokens(string userId)
    {
        User? user = await userManager.FindByIdAsync(userId);
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
            logger.LogError("User ID is null or empty, cannot revoke tokens.");
            return false;
        }

        // Find all refresh tokens associated with the user
        List<RefreshToken> userTokens = await context.RefreshTokens
                                                      .Where(rt => rt.UserId == userId && !rt.IsRevoked)
                                                      .ToListAsync();

        if (userTokens.Count == 0)
        {
            logger.LogWarning("No active refresh tokens found for the user.");
            return false;
        }

        // Revoke all refresh tokens
        foreach (RefreshToken? token in userTokens)
        {
            token.IsRevoked = true;
        }

        context.RefreshTokens.UpdateRange(userTokens);
        await context.SaveChangesAsync();
        logger.LogInformation("All refresh tokens for user have been revoked.");

        return true;
    }
}