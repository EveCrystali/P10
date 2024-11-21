using Frontend.Models;
namespace Frontend.Services;

public static class DiabetesRiskPredictionService
{
    public static PatientRiskRequest MapPatientViewModelToPatientRiskInfo(PatientViewModel patientViewModel) => new()
    {
        Id = patientViewModel.PatientId,
        DateOfBirth = patientViewModel.DateOfBirth,
        Gender = patientViewModel.Gender
    };
}