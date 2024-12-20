using Auth.Data;
using Auth.Models;
using Auth.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Auth.Controllers;

[Route("user")]
[ApiController]
public class UserController(ApplicationDbContext context, UserManager<User> userManager, IJwtRevocationService jwtRevocationService) : ControllerBase
{

    [HttpGet]
    public async Task<IActionResult> GetUsers()
    {
        List<User> users = await context.Users.ToListAsync();
        return Ok(users);
    }

    [Authorize(Policy = "RequireUserRole")]
    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(string id)
    {
        User? user = await context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound("No User found with this Id");
        }

        return Ok(user.Id);
    }

    [Authorize(Policy = "RequireUserRole")]
    [HttpGet("username/{username}")]
    public async Task<ActionResult<string>> GetUserIdFromUsername(string username)
    {
        User? user = await context.Users.FirstOrDefaultAsync(u => u.UserName == username);

        if (user == null)
        {
            return NotFound("No User found with this username");
        }

        return Ok(user.Id);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutUser(string id, UserUpdateModel userModel)
    {
        if (id != userModel.Id)
        {
            return BadRequest("The Id entered in the parameter is not the same as the Id enter in the body");
        }

        User? currentUser = await userManager.GetUserAsync(HttpContext.User);
        User? existingUser = await userManager.FindByIdAsync(id);

        if (currentUser == null)
        {
            return Unauthorized("You are not authorized to perform this action.");
        }

        if (existingUser == null)
        {
            return NotFound("User with this Id does not exist.");
        }

        if (currentUser.Id != userModel.Id && !await userManager.IsInRoleAsync(currentUser, "Admin"))
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        existingUser.Email = userModel.Email;

        if ((userModel.Role == "User" || userModel.Role == "Practitioner") && await userManager.IsInRoleAsync(currentUser, "Admin"))
        {
            IActionResult? roleUpdateResult = await UpdateUserRole(existingUser, userModel.Role);
            if (roleUpdateResult != null)
            {
                return roleUpdateResult;
            }
        }

        IdentityResult result = await userManager.UpdateAsync(existingUser);
        if (!result.Succeeded)
        {
            return BadRequest(result.Errors);
        }

        return Ok(existingUser);
    }

    private async Task<IActionResult?> UpdateUserRole(User user, string newRole)
    {
        IList<string> currentRoles = await userManager.GetRolesAsync(user);

        if (!currentRoles.Contains(newRole))
        {
            IdentityResult removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                return BadRequest(removeResult.Errors);
            }

            IdentityResult addResult = await userManager.AddToRoleAsync(user, newRole);
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
        User? currentUser = await userManager.GetUserAsync(HttpContext.User);
        if (currentUser == null)
        {
            return Forbid();
        }

        bool isAdmin = await userManager.IsInRoleAsync(currentUser, "Admin");
        if (!isAdmin && id != currentUser.Id)
        {
            return Forbid();
        }

        User? userToDelete = await context.Users.FindAsync(id);
        if (userToDelete == null)
        {
            return NotFound();
        }

        await jwtRevocationService.RevokeUserTokensAsync(currentUser.Id);

        if (id == currentUser.Id)
        {
            await SignOutCurrentUserAsync();
        }

        context.Users.Remove(userToDelete);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private async Task SignOutCurrentUserAsync()
    {

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