namespace Frontend.Models;

public class PatientRiskRequest
{
    public int Id { get; set; }
    public required DateOnly DateOfBirth { get; set; }
    public required string Gender { get; set; }
}