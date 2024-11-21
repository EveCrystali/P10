using BackendNote.Models;
using BackendNote.Services;
using MongoDB.Driver;
namespace BackendNote.Data;

public class DataSeeder(NotesService notesService, ILogger<DataSeeder> logger)
{

    private readonly List<Note> _notesAtStartup = new()
    {
        // Patient 1
        CreateNote(1, "Le patient déclare qu'il 'se sent très bien' Poids égal ou inférieur au poids recommandé"),

        // Patient 2
        CreateNote(2, "Le patient déclare qu'il ressent beaucoup de stress au travail Il se plaint également que son audition est anormale dernièrement"),
        CreateNote(2, "Le patient déclare avoir fait une réaction aux médicaments au cours des 3 derniers mois Il remarque également que son audition continue d'être anormale"),

        // Patient 3
        CreateNote(3, "Le patient déclare qu'il fume depuis peu"),
        CreateNote(3, "Le patient déclare qu'il est fumeur et qu'il a cessé de fumer l'année dernière Il se plaint également de crises d'apnée respiratoire anormales Tests de laboratoire indiquant un taux de cholestérol LDL élevé"),

        // Patient 4
        CreateNote(4, "Le patient déclare qu'il lui est devenu difficile de monter les escaliers Il se plaint également d'être essoufflé Tests de laboratoire indiquant que les anticorps sont élevés Réaction aux médicaments"),
        CreateNote(4, "Le patient déclare qu'il a mal au dos lorsqu'il reste assis pendant longtemps"),
        CreateNote(4, "Le patient déclare avoir commencé à fumer depuis peu Hémoglobine A1C supérieure au niveau recommandé"),
        CreateNote(4, "Taille, Poids, Cholestérol, Vertige et Réaction")
    };

    private static Note CreateNote(int patientId, string body) => new()
    {
        PatientId = patientId,
        Title = "",
        Body = body
    };

    public async Task SeedNotesAsync()
    {
        List<Note> notes = _notesAtStartup;
        int batchSize = 10;
        List<Task> tasks = [];

        logger.LogInformation("Start seeding notes");

        for (int i = 0; i < notes.Count; i += batchSize)
        {
            IEnumerable<Note> batch = notes.Skip(i).Take(batchSize);

            foreach (Note note in batch)
            {
                tasks.Add(Task.Run(async () =>
                {
                    Note existingNote = await notesService.NotesCollection
                                                          .Find(n => n.Title == note.Title && n.Body == note.Body)
                                                          .FirstOrDefaultAsync();

                    if (existingNote == null)
                    {
                        await notesService.CreateAsync(note);
                        logger.LogDebug($"Note created with title {note.Title} and body {note.Body}");
                    }
                    else
                    {
                        logger.LogDebug($"Note with title {note.Title} and body {note.Body} already exists");
                    }
                }));
            }

            await Task.WhenAll(tasks);
            tasks.Clear();
        }

        logger.LogInformation("Finish seeding notes");
    }
}