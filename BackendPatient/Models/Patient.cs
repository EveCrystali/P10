using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace BackendPatient.Models;

public class Patient
{
    [Key]
    public int Id;

    [Required(ErrorMessage = "First name is required")]
    [Length(1,50, ErrorMessage = "First name should be between 1 and 50 characters")]
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    [Length(1, 50, ErrorMessage = "Last name should be between 1 and 50 characters")]
    public required string LastName { get; set; }

    [DataType(DataType.DateTime, ErrorMessage = "Date Of Birth must be a date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    [RegularExpression(@"^(19|20)\d\d[-](0[1-9]|1[012])[-](0[1-9]|[12][0-9]|3[01])$", ErrorMessage = "Not a valid date")]	
    public DateTime? DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [StringLength(1, ErrorMessage = "Gender should be either M or F")]
    [DisplayFormat(DataFormatString = "{0:M/F}", ApplyFormatInEditMode = true)]
    public string? Gender { get; set; }

    [DataType(DataType.Text)]
    [MaxLength(100, ErrorMessage = "Address can't be longer than 100 characters")]
    public string? Address { get; set; }

    [DataType(DataType.PhoneNumber)]
    [RegularExpression(@"^[0-9]{3}[-]([0-9]{3})[-]([0-9]{4})$", ErrorMessage = "Not a valid phone number")]
    public string? PhoneNumber { get; set; }


    public static Patient FormatPatient((string nom, string prenom, string? dateDeNaissance, string? genre,
                                        string? adresse, string? telephone) unformattedPatient)
    {
        Patient patient = new()
        {
            FirstName = unformattedPatient.nom,
            LastName = unformattedPatient.prenom,
            Gender = unformattedPatient.genre,
            Address = unformattedPatient.adresse,
            PhoneNumber = unformattedPatient.telephone
        };

        try
        {
            patient.DateOfBirth = DateTime.ParseExact(unformattedPatient.dateDeNaissance?.ToString() ?? string.Empty,
                                                    "yyyy-MM-dd", CultureInfo.InvariantCulture);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception occured while parsing dateOfBirth from patient "
                              + $"{unformattedPatient.nom} {unformattedPatient.prenom} with exception : {ex}");
            patient.DateOfBirth = null;
        }

        return patient;
    }
}
