using Auth.Data;
using Auth.Models;
using Auth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Auth.Controllers;

[Route("users")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    private readonly UserManager<User> _userManager;

    private readonly IJwtRevocationService _jwtRevocationService;

    public UserController(ApplicationDbContext context, UserManager<User> userManager, IJwtRevocationService jwtRevocationService)
    {
        _context = context;
        _userManager = userManager;
        _jwtRevocationService = jwtRevocationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        List<User> users = await _context.Users.ToListAsync();
        return users != null ? Ok(users) : BadRequest("Failed to get list of Users");
    }

    [Authorize(Policy = "RequireUserRole")]
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(string id)
    {
        User? user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound("No User found with this Id");
        }

        return Ok(user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutUser(string id, UserUpdateModel userModel)
    {
        if (id != userModel.Id)
        {
            return BadRequest("The Id entered in the parameter is not the same as the Id enter in the body");
        }

        User? currentUser = await _userManager.GetUserAsync(HttpContext.User);
        User? existingUser = await _userManager.FindByIdAsync(id);

        if (currentUser == null)
        {
            return Unauthorized("You are not authorized to perform this action.");
        }

        if (existingUser == null)
        {
            return NotFound("User with this Id does not exist.");
        }

        if (currentUser.Id != userModel.Id && !await _userManager.IsInRoleAsync(currentUser, "Admin"))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        existingUser.Email = userModel.Email;

        if ((userModel.Role == "User" || userModel.Role == "Practitioner") && await _userManager.IsInRoleAsync(currentUser, "Admin"))
        {
            IActionResult? roleUpdateResult = await UpdateUserRole(existingUser, userModel.Role);
            if (roleUpdateResult != null)
            {
                return roleUpdateResult;
            }
        }

        IdentityResult result = await _userManager.UpdateAsync(existingUser);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(existingUser);
    }

    private async Task<IActionResult?> UpdateUserRole(User user, string newRole)
    {
        IList<string> currentRoles = await _userManager.GetRolesAsync(user);

        if (!currentRoles.Contains(newRole))
        {
            IdentityResult removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                return BadRequest(removeResult.Errors);
            }

            IdentityResult addResult = await _userManager.AddToRoleAsync(user, newRole);
            if (!addResult.Succeeded)
            {
                return BadRequest(addResult.Errors);
            }
        }
        return null;
    }

    [Authorize(Policy = "RequireAdminRole")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        User? currentUser = await _userManager.GetUserAsync(HttpContext.User);
        if (currentUser == null)
        {
            return Forbid();
        }

        bool isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
        if (!isAdmin && id != currentUser.Id)
        {
            return Forbid();
        }

        User? userToDelete = await _context.Users.FindAsync(id);
        if (userToDelete == null)
        {
            return NotFound();
        }

        await _jwtRevocationService.RevokeUserTokensAsync(currentUser.Id);

        if (id == currentUser.Id)
        {
            await SignOutCurrentUserAsync();
        }

        _context.Users.Remove(userToDelete);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task SignOutCurrentUserAsync()
    {
        if (HttpContext?.RequestServices == null)
        {
            return;
        }

        IAuthenticationService? authService = HttpContext.RequestServices.GetService<IAuthenticationService>();
        if (authService == null)
        {
            return;
        }

        try
        {
            await authService.SignOutAsync(HttpContext, IdentityConstants.ApplicationScheme, null);
        }
        catch (InvalidOperationException)
        {
            // Ignore exception in test environnement
        }
    }
}