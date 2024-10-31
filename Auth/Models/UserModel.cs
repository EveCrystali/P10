using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
namespace Auth.Models;

public class User : IdentityUser
{
    [DataType(DataType.DateTime, ErrorMessage = "LastLoginDate must be a date and a time of day")]
    public DateTime? LastLoginDate { get; set; }
}

public static class UserExtensions
{
    public static bool IsUserActive(this User user)
    {
        if (user.LastLoginDate == null)
        {
            return false;
        }

        return user.LastLoginDate >= DateTime.UtcNow.AddYears(-2);
    }
}