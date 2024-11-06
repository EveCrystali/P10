namespace BackendDiabetesRiskPrediction.Models;

public class PatientRiskInfo
{
    public int Id { get; set; }
    public required DateOnly DateOfBirth { get; set; }
    public required string Gender { get; set; }
}