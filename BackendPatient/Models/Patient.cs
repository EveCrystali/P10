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
    public required string FirstName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    public required string LastName { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
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
