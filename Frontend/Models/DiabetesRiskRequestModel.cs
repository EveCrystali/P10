namespace Frontend.Models;

public class DiabetesRiskRequestModel
{
    public List<NoteRiskInfo>? NotesRiskInfo { get; set; }
    public PatientRiskInfo? PatientRiskInfo { get; set; }
}