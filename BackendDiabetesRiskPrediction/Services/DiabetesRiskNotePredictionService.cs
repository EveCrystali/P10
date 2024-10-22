using BackendDiabetesRiskPrediction.Models;

namespace BackendDiabetesRiskPrediction.Services;

public class DiabetesRiskNotePredictionService(ILogger<DiabetesRiskNotePredictionService> logger)
{

    private readonly ILogger<DiabetesRiskNotePredictionService> _logger = logger;

    public int DiabetesRiskPredictionNotesAnalysis(List<NoteRiskInfo> notes)
    {
        int triggersDiabetesRiskFromNotes = 0;

        foreach (NoteRiskInfo note in notes)
        {
            triggersDiabetesRiskFromNotes += DiabetesRiskPredictionSingleNoteAnalysis(note);
        }
        return triggersDiabetesRiskFromNotes;
    }

    private static string DiabetesRiskPredictionSingleNoteTransform(NoteRiskInfo note)
    {
        return note.Title?.ToLower() + note.Body?.ToLower();
    }

    private int DiabetesRiskPredictionSingleNoteAnalysis(NoteRiskInfo note)
    {
        int triggersDiabetesRiskFromNote = 0;
        DiabetesRiskPredictionSingleNoteTransform(note);

        HashSet<string> triggers = TriggerWordsMix(triggerWords);

        // TODO: implement diabetes risk prediction based on patient single note

        return triggersDiabetesRiskFromNote;
    }

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

    private static HashSet<string> TriggerWordsMix(HashSet<string> triggerWords)
    {
        HashSet<string> triggerWordsMix = [.. triggerWords];
        foreach (string triggerWord in triggerWordsMix)
        {
            triggerWordsMix.Add(triggerWord.ToLower());
        }

        return triggerWordsMix;
    }

    public DiabetesRisk DiabetesRiskPrediction(List<NoteRiskInfo> notes, PatientRiskInfo patientRiskInfo)
    {
        DiabetesRisk diabetesRisk;

        if (notes == null)
        {
            diabetesRisk = DiabetesRisk.None;
            _logger.LogWarning("No notes found or an error occurred during prediction.");
            return diabetesRisk;
        }

        int triggersDiabetesRiskFromNotes = DiabetesRiskPredictionNotesAnalysis(notes);

        diabetesRisk = DiabetesRiskPredictionCalculator(patientRiskInfo, triggersDiabetesRiskFromNotes);

        return diabetesRisk;
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
        else
        {
            return DiabetesRiskPredictionFor30AndOlder(triggersDiabetesRiskFromNotes);
        }
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
            else if (triggersDiabetesRiskFromNotes >= 3)
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

    private static int PatientAgeCalculator(PatientRiskInfo patientRiskInfo)
    {
        DateTime currentDate = DateTime.Now;
        DateOnly birthDate = patientRiskInfo.DateOfBirth;
        int age = currentDate.Year - birthDate.Year;
        return age;
    }
}