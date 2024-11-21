using BackendDiabetesRiskPrediction.Models;
namespace BackendDiabetesRiskPrediction.Services;

public class DiabetesRiskNotePredictionService(ElasticsearchService elasticsearchService, ILogger<DiabetesRiskNotePredictionService> logger)
{

    private readonly HashSet<string> _triggerWords =
    [
        "Hémoglobine A1C",
        "Microalbumine",
        "Taille",
        "Poids",
        "Fumer",
        "Anormal",
        "Cholestérol",
        "Vertiges",
        "Rechute",
        "Réaction",
        "Anticorps"
    ];

    public async Task<DiabetesRiskPrediction> DiabetesRiskPrediction(List<NoteRiskInfo>? notes, PatientRiskInfo? patientRiskInfo)
    {
        DiabetesRiskPrediction diabetesRiskPrediction = new();
        if (notes is null || patientRiskInfo is null)
        {
            diabetesRiskPrediction.DiabetesRisk = DiabetesRisk.None;
            return diabetesRiskPrediction;
        }

        int triggersDiabetesRiskFromNotes = await DiabetesRiskPredictionNotesAnalysis(patientRiskInfo.Id, _triggerWords);

        diabetesRiskPrediction.DiabetesRisk = DiabetesRiskPredictionCalculator(patientRiskInfo, triggersDiabetesRiskFromNotes);

        return diabetesRiskPrediction;
    }


    private async Task<int> DiabetesRiskPredictionNotesAnalysis(int patientId, HashSet<string> hashSetofTriggerWords) => await elasticsearchService.CountUniqueWordsInNotes(patientId, hashSetofTriggerWords);

    private DiabetesRisk DiabetesRiskPredictionCalculator(PatientRiskInfo patientRiskInfo, int triggersDiabetesRiskFromNotes)
    {
        int age = PatientAgeCalculator(patientRiskInfo);

        // Do not need to consider if Female or Male or Age
        if (triggersDiabetesRiskFromNotes == 0)
        {
            return DiabetesRisk.None;
        }

        // Patient is younger than 30 (exclusive)
        return age < 30
            ? DiabetesRiskPredictionForUnder30(patientRiskInfo, triggersDiabetesRiskFromNotes)
            :
            // Patient is older (or equal) than 30 (inclusive)
            DiabetesRiskPredictionFor30AndOlder(triggersDiabetesRiskFromNotes);
    }

    private DiabetesRisk DiabetesRiskPredictionForUnder30(PatientRiskInfo patientRiskInfo, int triggersDiabetesRiskFromNotes)
    {
        logger.LogInformation($"Patient gender is : {patientRiskInfo.Gender}");

        return patientRiskInfo.Gender switch
        {
            // Patient is a male
            // Let's consider triggers from notes
            "M" when triggersDiabetesRiskFromNotes >= 5 => DiabetesRisk.EarlyOnset,
            "M" when triggersDiabetesRiskFromNotes >= 3 => DiabetesRisk.InDanger,
            // Patient is a female
            // Let's consider triggers from notes
            "F" when triggersDiabetesRiskFromNotes >= 7 => DiabetesRisk.EarlyOnset,
            "F" when triggersDiabetesRiskFromNotes >= 4 => DiabetesRisk.InDanger,
            _                                           => DiabetesRisk.None
        };
    }

    private static DiabetesRisk DiabetesRiskPredictionFor30AndOlder(int triggersDiabetesRiskFromNotes)
    {
        return triggersDiabetesRiskFromNotes switch
        {
            >= 8 => DiabetesRisk.EarlyOnset,
            // Equivalent to trigger >= 6 && trigger <= 7 due to the order of the if statements
            >= 6 => DiabetesRisk.InDanger,
            // Equivalent to trigger >= 2 && trigger <= 5 due to the order of the if statements
            >= 2 => DiabetesRisk.Borderline,
            // Equivalent to trigger >= 0 && trigger <= 1 due to the order of the if statements
            _ => DiabetesRisk.None
        };

    }

    private int PatientAgeCalculator(PatientRiskInfo patientRiskInfo)
    {
        DateTime currentDate = DateTime.Now;
        DateOnly birthDate = patientRiskInfo.DateOfBirth;
        int age = currentDate.Year - birthDate.Year;
        logger.LogInformation("Patient age is : {Age}", age);
        return age;
    }
}