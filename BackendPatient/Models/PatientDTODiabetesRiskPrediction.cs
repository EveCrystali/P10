using System;
using System.ComponentModel.DataAnnotations;
namespace BackendPatient.Models;

public class PatientDtoDiabetesRiskPrediction
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Date of birth is required")]
    [DataType(DataType.Date, ErrorMessage = "Date Of Birth must be a date")]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
    public required DateOnly DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [StringLength(1, ErrorMessage = "Gender should be either M or F")]
    [DisplayFormat(DataFormatString = "{0:M/F}", ApplyFormatInEditMode = true)]
    [RegularExpression(@"^[MF]$", ErrorMessage = "Gender should be either M or F")]
    public required string Gender { get; set; }
}

