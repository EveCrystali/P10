namespace BackendDiabetesRiskPrediction.Models;

public class NoteRiskInfo
{
    public string? Id { get; set; }

    public required int PatientId { get; set; }

    public string? Title { get; set; }

    public string? Body { get; set; }
}