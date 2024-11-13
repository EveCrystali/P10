using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace Frontend.Models;

public class Patient
{
    public int Id { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [Length(1, 50, ErrorMessage = "First name should be between 1 and 50 characters")]
    [DisplayName(displayName:"Prénom")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    [Length(1, 50, ErrorMessage = "Last name should be between 1 and 50 characters")]
    [DisplayName(displayName:"Nom")]
    public required string LastName { get; set; }

    [Required(ErrorMessage = "Date of birth is required")]
    [DataType(DataType.Date, ErrorMessage = "Date Of Birth must be a date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [DisplayName(displayName:"Date de naissance")]
    public required DateOnly DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [StringLength(1, ErrorMessage = "Gender should be either M or F")]
    [DisplayFormat(DataFormatString = "{0:M/F}", ApplyFormatInEditMode = true)]
    [RegularExpression(@"^[MF]$", ErrorMessage = "Gender should be either M or F")]
    [DisplayName(displayName:"Genre (M/F)")]
    public required string Gender { get; set; }

    [DataType(DataType.Text)]
    [MaxLength(100, ErrorMessage = "Address can't be longer than 100 characters")]
    [DisplayName(displayName:"Adresse")]
    public string? Address { get; set; }

    [DataType(DataType.PhoneNumber)]
    [RegularExpression(@"^[0-9]{3}[-]([0-9]{3})[-]([0-9]{4})$", ErrorMessage = "Not a valid phone number")]
    [DisplayName(displayName:"Tél.")]
    public string? PhoneNumber { get; set; }
}