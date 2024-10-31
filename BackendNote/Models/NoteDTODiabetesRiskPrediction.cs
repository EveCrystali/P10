namespace BackendNote.Models;

public class NoteDtoDiabetesRiskPrediction
{
    public string? Id { get; set; }

    public required int? PatientId { get; set; }

    public string? Title { get; set; }

    public string? Body { get; set; }
}