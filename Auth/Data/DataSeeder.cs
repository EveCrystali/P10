using System;
using Microsoft.AspNetCore.Identity;
using Auth.Models;

namespace Auth.Data;

public class DataSeeder
{
    private static readonly Dictionary<string, string[]> usersRolesPasswords = new()
    {
        { "Admin", ["admin@email.com",  "0vBZBB.QH83GeE."]},
        { "Practitioner", ["practitioner@email.com", "1vBZBB.QH83GeE."] },
        { "User", ["noroleuser@email.com", "2vBZBB.QH83GeE."]}
    };

    public static async Task SeedUsers(UserManager<User> userManager, ILogger logger)
    {
        IEnumerable<Task> tasks = usersRolesPasswords.Select(async userToAdd =>
        {
            if (await userManager.FindByEmailAsync(userToAdd.Value[0]) == null)
            {
                User newUser = new() { UserName = userToAdd.Value[0], Email = userToAdd.Value[0], EmailConfirmed = true, LockoutEnabled = false };
                await userManager.CreateAsync(newUser, userToAdd.Value[1]);
                logger.LogInformation($"Created new user with email: {userToAdd.Value[0]}");
            }
            else
            {
                string messageLog = $"The user with the name {userToAdd.Value[0]} already exists.";
                logger.LogInformation(messageLog);
            }
        }
        );
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Seeds the roles in the database.
    /// </summary>
    /// <param name="roleManager">The role manager.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
    {
        // Loop through each role name and see if the role already exists.
        IEnumerable<Task> tasks = usersRolesPasswords.Keys.Select(async roleName =>
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        });
        await Task.WhenAll(tasks); 
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
    public static async Task SeedAffectationsRolesToUsers(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        IEnumerable<Task> tasks = usersRolesPasswords.Select(async userToAffect =>
        {
            User? user = await userManager.FindByEmailAsync(userToAffect.Value[0]);
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
        });
        
        await Task.WhenAll(tasks); 
    }
}

