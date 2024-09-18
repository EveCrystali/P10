using System;
using Microsoft.AspNetCore.Identity;
using Auth.Models;

namespace Auth.Data;

public class DataSeeder
{
    private const string AdminEmail = "admin@email.com";
    private const string PractitionerEmail = "practitioner@email.com";
    private const string NoRoleUserEmail = "noroleuser@email.com";
    private static readonly string[] emails = [AdminEmail, PractitionerEmail, NoRoleUserEmail];

    // TODO:  refactor following seed methods to use the Dictionary
    private static readonly Dictionary<string, string> usersAndRoles = new()
    {
        { "admin@email.com", "Admin" },
        { "practitioner@email.com", "Practitioner" },
        { "noroleuser@email.com", "User" }
    };

    public static readonly string[] roleNames = ["Admin", "Practitioner", "User"];
    public static async Task SeedUsers(UserManager<User> userManager)
    {
        IdentityUser? adminUser = await userManager.FindByEmailAsync("admin@email.com");
        if (adminUser == null)
        {
            User newUser = new() { UserName = "admin@email.com", Email = "admin@email.com", EmailConfirmed = true, LockoutEnabled = false };
            await userManager.CreateAsync(newUser, "0vBZBB.QH83GeE.");
        }

        IdentityUser? practitionerUser = await userManager.FindByEmailAsync("practitioner@email.com");
        if (practitionerUser == null)
        {
            User newUser = new() { UserName = "practitioner@email.com", Email = "practitioner@email.com", EmailConfirmed = true, LockoutEnabled = false };
            await userManager.CreateAsync(newUser, "1vBZBB.QH83GeE.");
        }

        IdentityUser? noRoleUser = await userManager.FindByEmailAsync("noroleuser@email.com");
        if (noRoleUser == null)
        {
            User newUser = new() { UserName = "noroleuser@email.com", Email = "noroleuser@email.com", EmailConfirmed = true, LockoutEnabled = false };
            await userManager.CreateAsync(newUser, "2vBZBB.QH83GeE.");
        }
    }

    /// <summary>
    /// Seeds the roles in the database.
    /// </summary>
    /// <param name="roleManager">The role manager.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task SeedRoles(RoleManager<IdentityRole> roleManager)
    {
        // Loop through each role name and see if the role already exists.
        foreach (string roleName in roleNames)
        {
            // If the role does not exist, create it.
            bool roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }


    // TODO: Verify the logic of this method
    /// <summary>
    /// Seeds the affectations of roles to users.
    /// </summary>
    /// <param name="userManager">The user manager.</param>
    /// <param name="roleManager">The role manager.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public static async Task SeedAffectationsRolesToUsers(UserManager<User> userManager, RoleManager<IdentityRole> roleManager, ILogger logger)
    {
        // Get the users
        
        // User? userAdmin = await userManager.FindByNameAsync(AdminEmail);
        // User? userPractitioner = await userManager.FindByNameAsync(PractitionerEmail);
        // User? userNoRoleUser = await userManager.FindByNameAsync(NoRoleUserEmail);
        // List<User?> users = [userAdmin, userPractitioner, userNoRoleUser];
        int i = 0;
        
        foreach (string email in emails) 
        {
            User? user = await userManager.FindByNameAsync(email);
            if (user != null)
            {
                await userManager.AddToRoleAsync(user, roleNames[i]);
            }
            else
            {
                // Log an error if the user was not found.
                string messageLog = $"The user with the email emails[] was not found.";
                logger.LogError(messageLog);
            }
            i++;
        }
    }

}

