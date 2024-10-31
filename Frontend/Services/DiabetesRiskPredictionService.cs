using Frontend.Models;
namespace Frontend.Services;

public class DiabetesRiskPredictionService
{
    public NoteRiskInfo MapPatientViewModelToNoteRiskInfo(Note note) => new()
    {
        Id = note.Id,
        PatientId = note.PatientId,
        Title = note.Title,
        Body = note.Body
    };

    public PatientRiskInfo MapPatientViewModelToPatientRiskInfo(PatientViewModel patientViewModel) => new()
    {
        Id = patientViewModel.PatientId,
        DateOfBirth = patientViewModel.DateOfBirth,
        Gender = patientViewModel.Gender
    };

    public DiabetesRiskRequestModel MapPatientViewModelAndNoteToDiabetesRiskRequestModel(PatientViewModel patientViewModel)
    {
        PatientRiskInfo patientRiskInfo = MapPatientViewModelToPatientRiskInfo(patientViewModel);
        List<NoteRiskInfo> noteRiskInfos = [];
        foreach (Note note in patientViewModel.Notes)
        {
            noteRiskInfos.Add(MapPatientViewModelToNoteRiskInfo(note));
        }

        return new DiabetesRiskRequestModel
        {
            NotesRiskInfo = noteRiskInfos,
            PatientRiskInfo = patientRiskInfo
        };
    }
}