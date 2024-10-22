using System.ComponentModel.DataAnnotations;
using System.Globalization;
using SharedLibrary.Extensions;

namespace BackendPatient.Models;

public class Patient : IValidatable
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "First name is required")]
    [Length(1, 50, ErrorMessage = "First name should be between 1 and 50 characters")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    [Length(1, 50, ErrorMessage = "Last name should be between 1 and 50 characters")]
    public required string LastName { get; set; }

    [Required(ErrorMessage = "Date of birth is required")]
    [DataType(DataType.Date, ErrorMessage = "Date Of Birth must be a date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public required DateOnly DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [StringLength(1, ErrorMessage = "Gender should be either M or F")]
    [DisplayFormat(DataFormatString = "{0:M/F}", ApplyFormatInEditMode = true)]
    [RegularExpression(@"^[MF]$", ErrorMessage = "Gender should be either M or F")]
    public required string Gender { get; set; }

    [DataType(DataType.Text)]
    [MaxLength(100, ErrorMessage = "Address can't be longer than 100 characters")]
    public string? Address { get; set; }

    [DataType(DataType.PhoneNumber)]
    [RegularExpression(@"^[0-9]{3}[-]([0-9]{3})[-]([0-9]{4})$", ErrorMessage = "Not a valid phone number")]
    public string? PhoneNumber { get; set; }

    // NOTE: This method is responsible for converting raw patient data (unformatted)
    // into a strongly typed Patient object.
    // This conversion is primarily used in the data seeding process or other operations
    // where patient data is imported from external sources.
    // This method is currently placed in the Patient class for convenience,
    // as it directly deals with creating a Patient object.
    // FUTURE: Consider moving this logic to a dedicated service or factory class
    // (e.g., PatientFactory) to respect the Single Responsibility Principle (SRP)
    // and improve separation of concerns as the application grows.
    public static Patient FormatPatient((string nom, string prenom, string? dateDeNaissance, string genre,
                                        string? adresse, string? telephone) unformattedPatient)
    {
        DateOnly dateOfBirthFormatted;
        try
        {
            dateOfBirthFormatted = DateOnly.ParseExact(unformattedPatient.dateDeNaissance?.ToString() ?? string.Empty,
                                                    "yyyy-MM-dd", CultureInfo.InvariantCulture);
            return new Patient
            {
                FirstName = unformattedPatient.nom,
                LastName = unformattedPatient.prenom,
                DateOfBirth = dateOfBirthFormatted,
                Gender = unformattedPatient.genre,
                Address = unformattedPatient.adresse,
                PhoneNumber = unformattedPatient.telephone
            };
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException("Exception occured while parsing dateOfBirth from patient "
                             + $"{unformattedPatient.nom} {unformattedPatient.prenom} with exception : {ex}");
        }
        catch (ArgumentNullException ex)
        {
            throw new InvalidOperationException($"An argument is null with exception : {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            throw new Exception("An unexpected exception occured : " + ex.Message);
        }
    }

    public void Validate()
    {
        ValidationExtensions.Validate(this);
    }
}