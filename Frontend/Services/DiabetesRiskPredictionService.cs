using Frontend.Models;
namespace Frontend.Services;

public static class DiabetesRiskPredictionService
{
    private static NoteRiskInfo MapPatientViewModelToNoteRiskInfo(Note note) => new()
    {
        Id = note.Id,
        PatientId = note.PatientId,
        Title = note.Title,
        Body = note.Body
    };

    private static PatientRiskInfo MapPatientViewModelToPatientRiskInfo(PatientViewModel patientViewModel) => new()
    {
        Id = patientViewModel.PatientId,
        DateOfBirth = patientViewModel.DateOfBirth,
        Gender = patientViewModel.Gender
    };

    public static DiabetesRiskRequestModel MapPatientViewModelAndNoteToDiabetesRiskRequestModel(PatientViewModel patientViewModel)
    {
        PatientRiskInfo patientRiskInfo = MapPatientViewModelToPatientRiskInfo(patientViewModel);
        if (patientViewModel.Notes == null)
        {
            return new DiabetesRiskRequestModel
            {
                PatientRiskInfo = patientRiskInfo
            };
        }
        List<NoteRiskInfo> noteRiskInfos = patientViewModel.Notes
                                                           .Select(MapPatientViewModelToNoteRiskInfo)
                                                           .ToList();
        return new DiabetesRiskRequestModel
        {
            NotesRiskInfo = noteRiskInfos,
            PatientRiskInfo = patientRiskInfo
        };
    }
}