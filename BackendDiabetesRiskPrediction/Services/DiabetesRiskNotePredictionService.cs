using BackendDiabetesRiskPrediction.Models;
namespace BackendDiabetesRiskPrediction.Services;

public class DiabetesRiskNotePredictionService(ILogger<DiabetesRiskNotePredictionService> logger, ElasticsearchService elasticsearchService)
{

    private readonly ILogger<DiabetesRiskNotePredictionService> _logger = logger;

    private readonly HashSet<string> triggerWords =
    [
        "Hémoglobine A1C",
        "Microalbumine",
        "Taille",
        "Poids",
        "Fumeur",
        "Fumeuse",
        "Anormal",
        "Cholestérol",
        "Vertiges",
        "Rechute",
        "Réaction",
        "Anticorps"
    ];

    public async Task<DiabetesRisk> DiabetesRiskPrediction(List<NoteRiskInfo> notes, PatientRiskInfo patientRiskInfo)
    {
        DiabetesRisk diabetesRisk;

        int triggersDiabetesRiskFromNotes = await DiabetesRiskPredictionNotesAnalysis(notes);

        diabetesRisk = DiabetesRiskPredictionCalculator(patientRiskInfo, triggersDiabetesRiskFromNotes);

        return diabetesRisk;
    }

    private async Task<int> DiabetesRiskPredictionNotesAnalysis(List<NoteRiskInfo> notes)
    {
        IEnumerable<Task<int>> tasks = notes.Select(DiabetesRiskPredictionSingleNoteAnalysis);
        int[] results = await Task.WhenAll(tasks);
        return results.Sum();
    }


    private async Task<int> DiabetesRiskPredictionSingleNoteAnalysis(NoteRiskInfo note)
    {
        return await elasticsearchService.CountWordsInNotes(note.PatientId, triggerWords);
    }

    private static DiabetesRisk DiabetesRiskPredictionCalculator(PatientRiskInfo patientRiskInfo, int triggersDiabetesRiskFromNotes)
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

    private static DiabetesRisk DiabetesRiskPredictionForUnder30(PatientRiskInfo patientRiskInfo, int triggersDiabetesRiskFromNotes)
    {
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

    private static int PatientAgeCalculator(PatientRiskInfo patientRiskInfo)
    {
        DateTime currentDate = DateTime.Now;
        DateOnly birthDate = patientRiskInfo.DateOfBirth;
        int age = currentDate.Year - birthDate.Year;
        return age;
    }
}