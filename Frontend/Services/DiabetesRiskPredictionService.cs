using Frontend.Models;

namespace Frontend.Services;

public class DiabetesRiskPredictionService
{
    private readonly ILogger<DiabetesRiskPredictionService> _logger;

    public DiabetesRiskPredictionService(ILogger<DiabetesRiskPredictionService> logger)
    {
        _logger = logger;
    }

    // public DiabetesRisk DiabetesRiskPrediction(PatientNotesViewModel patientNotesViewModel)
    // {
    //     DiabetesRisk diabetesRisk;

    //     if (patientNotesViewModel.Notes == null)
    //     {
    //         diabetesRisk = DiabetesRisk.None;
    //         _logger.LogWarning("No notes found or an error occurred during prediction.");
    //         return diabetesRisk;
    //     }

    //     // int triggersDiabetesRiskFromNotes = DiabetesRiskPredictionNotesAnalysis(patientNotesViewModel.Notes);

    //     diabetesRisk = DiabetesRiskPredictionCalculator(patientNotesViewModel, triggersDiabetesRiskFromNotes);

    //     return diabetesRisk;
    // }

    private static DiabetesRisk DiabetesRiskPredictionCalculator(PatientNotesViewModel patientNotesViewModel, int triggersDiabetesRiskFromNotes)
    {
        int age = PatientAgeCalculator(patientNotesViewModel);

        // Do not need consider if Female or Male or Age
        if (triggersDiabetesRiskFromNotes == 0)
        {
            return DiabetesRisk.None;
        }

        // Patient is younger than 30 (exclusive)
        if (age < 30)
        {
            return DiabetesRiskPredictionForUnder30(patientNotesViewModel, triggersDiabetesRiskFromNotes);
        }
        // Patient is older (or equal) than 30 (inclusive)
        else
        {
            return DiabetesRiskPredictionFor30AndOlder(triggersDiabetesRiskFromNotes);
        }
    }

    private static DiabetesRisk DiabetesRiskPredictionForUnder30(PatientNotesViewModel patientNotesViewModel, int triggersDiabetesRiskFromNotes)
    {
        // Patient is a male
        if (patientNotesViewModel.Gender == "M")
        {
            // Let's consider triggers from notes
            if (triggersDiabetesRiskFromNotes >= 5)
            {
                return DiabetesRisk.EarlyOnset;
            }
            else if (triggersDiabetesRiskFromNotes >= 3)
            {
                return DiabetesRisk.InDanger;
            }
        }
        // Patient is a female
        else if (patientNotesViewModel.Gender == "F")
        {
            // Let's consider triggers from notes
            if (triggersDiabetesRiskFromNotes >= 7)
            {
                return DiabetesRisk.EarlyOnset;
            }
            else if (triggersDiabetesRiskFromNotes >= 4)
            {
                return DiabetesRisk.InDanger;
            }
        }
        return DiabetesRisk.None;
    }

    private static DiabetesRisk DiabetesRiskPredictionFor30AndOlder(int triggersDiabetesRiskFromNotes)
    {
        if (triggersDiabetesRiskFromNotes >= 8)
        {
            return DiabetesRisk.EarlyOnset;
        }
        // Equivalent to trigger >= 6 && trigger <= 7 due to the order of the if statements
        else if (triggersDiabetesRiskFromNotes >= 6)
        {
            return DiabetesRisk.InDanger;
        }
        // Equivalent to trigger >= 2 && trigger <= 5 due to the order of the if statements
        else if (triggersDiabetesRiskFromNotes >= 2)
        {
            return DiabetesRisk.Borderline;
        }
        // Equivalent to trigger >= 0 && trigger <= 1 due to the order of the if statements
        else
        {
            return DiabetesRisk.None;
        }
    }

    private static int PatientAgeCalculator(PatientNotesViewModel patientNotesViewModel)
    {
        DateTime currentDate = DateTime.Now;
        DateOnly birthDate = patientNotesViewModel.DateOfBirth;
        int age = currentDate.Year - birthDate.Year;
        return age;
    }
}