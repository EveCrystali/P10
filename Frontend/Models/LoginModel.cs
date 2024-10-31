using System.ComponentModel.DataAnnotations;
namespace Frontend.Models;

public class LoginModel
{
    [Required(ErrorMessage = "ErrorMissingUsername")]
    [DataType(DataType.EmailAddress, ErrorMessage = "Error Invalid Email address")]
    [Display(Name = "Nom d'utilisateur")]
    public required string Username { get; set; }

    [Required(ErrorMessage = "ErrorMissingPassword")]
    [DataType(DataType.Password)]
    [Display(Name = "Mot de passe")]
    public required string Password { get; set; }
}