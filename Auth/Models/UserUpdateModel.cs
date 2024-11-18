using System.ComponentModel.DataAnnotations;
namespace Auth.Models;

public class UserUpdateModel
{
    [Required(ErrorMessage = "Id is mandatory")]
    public required string Id { get; set; }

    [Required(ErrorMessage = "Email is mandatory")]
    [DataType(DataType.EmailAddress)]
    public required string Email { get; set; }

    [RegularExpression(@"^(User|Practitioner|Admin)$", ErrorMessage = "Invalid role")]
    public string? Role { get; set; }
}