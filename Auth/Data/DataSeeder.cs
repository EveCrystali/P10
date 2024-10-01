using Auth.Models;
using Microsoft.AspNetCore.Identity;

namespace Auth.Data;

public static class DataSeeder
{
    private static readonly Dictionary<string, string[]> usersRolesPasswords = new()
    {
        { "Admin", ["admin@email.com",  "0vBZBB.QH83GeE."]},
        { "Practitioner", ["practitioner@email.com", "1vBZBB.QH83GeE."] },
        { "User", ["noroleuser@email.com", "2vBZBB.QH83GeE."]}
    };

    /// <summary>
    /// Seeds the users in the database.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task SeedUsers(UserManager<IdentityUser> userManager, ILogger logger)
    {
        // Loop through each user and see if the user already exists.
        foreach (KeyValuePair<string, string[]> userToAdd in usersRolesPasswords)
        {
            if (await userManager.FindByEmailAsync(userToAdd.Value[0]) == null)
            {
                // Create a new user and add it to the database.
                User newUser = new() { UserName = userToAdd.Value[0], Email = userToAdd.Value[0], EmailConfirmed = true, LockoutEnabled = false };
                await userManager.CreateAsync(newUser, userToAdd.Value[1]);
                logger.LogInformation($"Created new user with email: {userToAdd.Value[0]}");
            }
            else
            {
                // Log a message if the user already exists.
                string messageLog = $"The user with the name {userToAdd.Value[0]} already exists.";
                logger.LogInformation(messageLog);
            }
        }
    }

    /// <summary>
    /// Seeds the roles in the database.
    /// </summary>
    /// <param name="roleManager">The role manager.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task SeedRoles(RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        // Loop through each role name and see if the role already exists.
        foreach (string roleName in usersRolesPasswords.Keys)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                // Create a new role and add it to the database.
                await roleManager.CreateAsync(new IdentityRole(roleName));
                string messageLog = $"Created new role with name: {roleName}";
                logger.LogInformation(messageLog);
            }
            else
            {
                // Log a message if the role already exists.
                string messageLog = $"The role with the name {roleName} already exists.";
                logger.LogInformation(messageLog);
            }
        }
    }

    /// <summary>
    /// Seeds the affectations of roles to users.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="roleManager">The role manager.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <remarks>
    /// This method goes through each user and their corresponding roles
    /// and adds the user to the role if it does not already exist.
    /// If the user is not found, it logs an error.
    /// </remarks>
    public static async Task SeedAffectationsRolesToUsers(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        foreach (KeyValuePair<string, string[]> userToAffect in usersRolesPasswords)
        {
            IdentityUser? user = await userManager.FindByEmailAsync(userToAffect.Value[0]);
            if (user != null)
            {
                if (!await userManager.IsInRoleAsync(user, userToAffect.Key))
                {
                    await userManager.AddToRoleAsync(user, userToAffect.Key);
                    logger.LogInformation($"Added role {userToAffect.Key} to user {userToAffect.Value[0]}.");
                }
                else
                {
                    logger.LogInformation($"User {userToAffect.Value[0]} already has the role {userToAffect.Key}.");
                }
            }
            else
            {
                logger.LogError($"The user with the email {userToAffect.Value[0]} was not found.");
            }
        }
    }
}