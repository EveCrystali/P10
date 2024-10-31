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

        if (notes == null)
        {
            diabetesRisk = DiabetesRisk.None;
            _logger.LogWarning("No notes found or an error occurred during prediction.");
            return diabetesRisk;
        }

        int triggersDiabetesRiskFromNotes = await DiabetesRiskPredictionNotesAnalysis(notes);

        diabetesRisk = DiabetesRiskPredictionCalculator(patientRiskInfo, triggersDiabetesRiskFromNotes);

        return diabetesRisk;
    }

    public async Task<int> DiabetesRiskPredictionNotesAnalysis(List<NoteRiskInfo> notes)
    {
        int triggersDiabetesRiskFromNotes = 0;

        foreach (NoteRiskInfo note in notes)
        {
            triggersDiabetesRiskFromNotes += await DiabetesRiskPredictionSingleNoteAnalysis(note);
        }
        return triggersDiabetesRiskFromNotes;
    }

    private async Task<int> DiabetesRiskPredictionSingleNoteAnalysis(NoteRiskInfo note)
    {
        int triggersDiabetesRiskFromNote = 0;

        HashSet<string> triggers = TriggerWordsMix(triggerWords);

        // TODO: implement diabetes risk prediction based on patient single note

        await elasticsearchService.CountWordsInNotes(note.PatientId, triggers);

        return triggersDiabetesRiskFromNote;
    }

    private static HashSet<string> TriggerWordsMix(HashSet<string> triggerWords)
    {
        HashSet<string> triggerWordsMix = [.. triggerWords];
        foreach (string triggerWord in triggerWordsMix)
        {
            triggerWordsMix.Add(triggerWord.ToLower());
        }

        return triggerWordsMix;
    }

    private static DiabetesRisk DiabetesRiskPredictionCalculator(PatientRiskInfo patientRiskInfo, int triggersDiabetesRiskFromNotes)
    {
        int age = PatientAgeCalculator(patientRiskInfo);

        // Do not need consider if Female or Male or Age
        if (triggersDiabetesRiskFromNotes == 0)
        {
            return DiabetesRisk.None;
        }

        // Patient is younger than 30 (exclusive)
        if (age < 30)
        {
            return DiabetesRiskPredictionForUnder30(patientRiskInfo, triggersDiabetesRiskFromNotes);
        }
        // Patient is older (or equal) than 30 (inclusive)
        return DiabetesRiskPredictionFor30AndOlder(triggersDiabetesRiskFromNotes);
    }

    private static DiabetesRisk DiabetesRiskPredictionForUnder30(PatientRiskInfo patientRiskInfo, int triggersDiabetesRiskFromNotes)
    {
        // Patient is a male
        if (patientRiskInfo.Gender == "M")
        {
            // Let's consider triggers from notes
            if (triggersDiabetesRiskFromNotes >= 5)
            {
                return DiabetesRisk.EarlyOnset;
            }
            if (triggersDiabetesRiskFromNotes >= 3)
            {
                return DiabetesRisk.InDanger;
            }
        }
        // Patient is a female
        else if (patientRiskInfo.Gender == "F")
        {
            // Let's consider triggers from notes
            if (triggersDiabetesRiskFromNotes >= 7)
            {
                return DiabetesRisk.EarlyOnset;
            }
            if (triggersDiabetesRiskFromNotes >= 4)
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
        if (triggersDiabetesRiskFromNotes >= 6)
        {
            return DiabetesRisk.InDanger;
        }
        // Equivalent to trigger >= 2 && trigger <= 5 due to the order of the if statements
        if (triggersDiabetesRiskFromNotes >= 2)
        {
            return DiabetesRisk.Borderline;
        }
        // Equivalent to trigger >= 0 && trigger <= 1 due to the order of the if statements
        return DiabetesRisk.None;
    }

    private static int PatientAgeCalculator(PatientRiskInfo patientRiskInfo)
    {
        DateTime currentDate = DateTime.Now;
        DateOnly birthDate = patientRiskInfo.DateOfBirth;
        int age = currentDate.Year - birthDate.Year;
        return age;
    }
}