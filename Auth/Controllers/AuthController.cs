using Auth.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Controllers;

[Route("auth")]
[ApiController]
public class AuthController(
    UserManager<IdentityUser> userManager,
    SignInManager<IdentityUser> signInManager,
    ILogger<AuthController> logger) : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager = userManager;
    private readonly SignInManager<IdentityUser> _signInManager = signInManager;
    private readonly ILogger<AuthController> _logger = logger;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginModel model)
    {
        _logger.LogInformation($"Login request in Auth microservice from Auth controller in Login method with username: {model.Username}");
        IdentityUser? user = await _userManager.FindByNameAsync(model.Username);
        if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
        {
            _logger.LogInformation($"User {model.Username} found and password is correct");
            Microsoft.AspNetCore.Identity.SignInResult? result = await _signInManager.PasswordSignInAsync(user, model.Password, false, false);
            return Ok("Logged in successfully");
        }
        _logger.LogError($"User {model.Username} not found or password is incorrect");
        return Unauthorized("Invalid username or password");
    }

    [HttpPost]
    [Route("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return Ok("Logged out successfully");
    }

    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register([FromBody] RegisterModel model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        IdentityUser user = new() { UserName = model.Email, Email = model.Email };
        IdentityResult? result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return BadRequest(ModelState);
        }

        await _signInManager.SignInAsync(user, isPersistent: false);

        return Ok("Registered successfully");
    }
}