using System;
using System.ComponentModel.DataAnnotations;

namespace BackendDiabetesRiskPrediction.Models;

public class PatientRiskInfo
{
    public int Id { get; set; }
    public required DateOnly DateOfBirth { get; set; }
    public required string Gender { get; set; }

    [EnumDataType(typeof(DiabetesRisk))]
    public DiabetesRisk? DiabetesRisk { get; set; }
}