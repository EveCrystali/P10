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
    SignInManager<User> signInManager, IJwtService jwtService,
    ILogger<AuthController> logger, ApplicationDbContext context) : ControllerBase
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly SignInManager<User> _signInManager = signInManager;
    private readonly ILogger<AuthController> _logger = logger;
    private readonly IJwtService _jwtService = jwtService;
    private readonly ApplicationDbContext _context = context;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        string messageLog = "Login request in Auth microservice from Auth controller in Login method";
        _logger.LogInformation(messageLog);
        User? user = await _userManager.FindByNameAsync(model.Username);
        if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
        {
            _logger.LogInformation("User found, updating last login date.");
            user.LastLoginDate = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("Generating JWT token and refresh token.");
            IList<string> userRoles = await _userManager.GetRolesAsync(user);
            string token = _jwtService.GenerateToken(user.Id, user.UserName, userRoles.ToArray());
            RefreshToken refreshToken = _jwtService.GenerateRefreshToken(user.Id);

            _logger.LogInformation("Saving refresh token to the database.");
            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Login successful, returning tokens.");
            await _signInManager.SignInAsync(user, isPersistent: false);

            return Ok(new { Token = token, RefreshToken = refreshToken.Token });
        }
        else if (user == null)
        {
            _logger.LogError($"User not found");
            return NotFound("User not found");
        }
        else if (!await _userManager.CheckPasswordAsync(user, model.Password))
        {
            _logger.LogError($"User found but password is incorrect");
            return Unauthorized("Invalid username or password");
        }
        else
        {
            _logger.LogError("Something went wrong");
            return StatusCode(500, "Something went wrong");
        }
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

        // Get User ID from JWT token
        string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return BadRequest("Impossible to get user ID.");
        }

        // Revoke all refresh tokens for the user
        RefreshToken? currentToken = await GetCurrentRefreshTokenAsync(userId);
        if (currentToken == null)
        {
            _logger.LogError("Impossible to get current token.");
            return BadRequest("Impossible to get current token.");
        }

        currentToken.IsRevoked = true;
        _context.RefreshTokens.Update(currentToken);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Logout successful" });
    }

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        User user = new() { UserName = model.Username, Email = model.Email, PasswordHash = model.Password };
        IdentityResult result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(user, "User");
            return Ok(new { Message = "User registered successfully" });
        }
        return BadRequest(result.Errors);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] Auth.Models.RefreshRequest model)
    {
        RefreshToken? refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == model.RefreshToken);
        if (refreshToken == null || refreshToken.ExpiryDate < DateTime.UtcNow || refreshToken.IsRevoked)
        {
            return Unauthorized();
        }

        User? user = await _userManager.FindByIdAsync(refreshToken.UserId);
        if (user == null || !user.IsUserActive())
        {
            return Unauthorized();
        }

        IList<string> userRoles = await _userManager.GetRolesAsync(user);
        string newToken = _jwtService.GenerateToken(user.Id, user.UserName, userRoles.ToArray());
        RefreshToken newRefreshToken = _jwtService.GenerateRefreshToken(user.Id);

        // Revok previous refresh token
        refreshToken.IsRevoked = true;
        _context.RefreshTokens.Update(refreshToken);

        // Add new refresh token
        _context.RefreshTokens.Add(newRefreshToken);

        // Delete old refresh tokens
        List<RefreshToken> oldTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == user.Id && (rt.ExpiryDate < DateTime.UtcNow || rt.IsRevoked))
            .ToListAsync();
        _context.RefreshTokens.RemoveRange(oldTokens);

        await _context.SaveChangesAsync();

        return Ok(new { Token = newToken, RefreshToken = newRefreshToken.Token });
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpPost("revoke")]
    public async Task<IActionResult> RevokeTokens([FromBody] RevokeTokensRequest model)
    {
        User? user = await _userManager.FindByIdAsync(model.UserId);
        if (user == null)
        {
            return NotFound();
        }

        List<RefreshToken> userTokens = [.. _context.RefreshTokens.Where(rt => rt.UserId == model.UserId)];
        foreach (RefreshToken? token in userTokens)
        {
            token.IsRevoked = true;
        }

        _context.RefreshTokens.UpdateRange(userTokens);
        await _context.SaveChangesAsync();

        return Ok();
    }

    [HttpGet]
    [Route("status")]
    public IActionResult Status()
    {
        bool isAuthenticated = User.Identity?.IsAuthenticated ?? false;
        string? username = isAuthenticated ? User.Identity?.Name : null;
        return Ok(new { isAuthenticated, username });
    }

    private async Task<RefreshToken> GetCurrentRefreshTokenAsync(string userId)
    {
        RefreshToken? refreshToken = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .OrderByDescending(rt => rt.ExpiryDate)
            .FirstOrDefaultAsync() ?? throw new InvalidOperationException("No valid refresh token found for the user.");
        return refreshToken;
    }
}