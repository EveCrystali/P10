namespace BackendDiabetesRiskPrediction.Models;

public class DiabetesRiskRequest
{
    public List<NoteRiskInfo> NotesRiskInfo { get; set; }
    public PatientRiskInfo PatientRiskInfo { get; set; }
}