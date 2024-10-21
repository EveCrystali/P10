using System;
using BackendNote.Models;

namespace BackendNote.Services;

public class DiabetesRiskNotePredictionService
{
    private int DiabetesRiskPredictionNotesAnalysis(List<Note> notes)
    {
        int triggersDiabetesRiskFromNotes = 0;

        foreach (Note note in notes)
        {
            triggersDiabetesRiskFromNotes += DiabetesRiskPredictionSingleNoteAnalysis(note);
        }
        return triggersDiabetesRiskFromNotes;
    }

    private static string DiabetesRiskPredictionSingleNoteTransform(Note note)
    {
        return note.Title?.ToLower() + note.Body?.ToLower();
    }

    private int DiabetesRiskPredictionSingleNoteAnalysis(Note note)
    {
        int triggersDiabetesRiskFromNote = 0;
        DiabetesRiskPredictionSingleNoteTransform(note);

        HashSet<string> triggers = TriggerWordsMix(triggerWords);

        // TODO: implement diabetes risk prediction based on patient single note

        return triggersDiabetesRiskFromNote;
    }

    readonly HashSet<string> triggerWords =
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
}
