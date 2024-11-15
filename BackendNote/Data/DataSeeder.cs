using BackendNote.Models;
using BackendNote.Services;
using MongoDB.Driver;
namespace BackendNote.Data;

public class DataSeeder(NotesService notesService, ILogger<DataSeeder> logger)
{
    private readonly List<Note> notesAtStartup = new()
    {
        new Note
        {
            PatientId = 1,
            Title = "",
            Body = "Le patient déclare qu'il 'se sent très bien' Poids égal ou inférieur au poids recommandé"
        },
        new Note
        {
            PatientId = 2,
            Title = "",
            Body = "Le patient déclare qu'il ressent beaucoup de stress au travail Il se plaint également que son audition est anormale dernièrement"
        },
        new Note
        {
            PatientId = 2,
            Title = "",
            Body = "Le patient déclare avoir fait une réaction aux médicaments au cours des 3 derniers mois Il remarque également que son audition continue d'être anormale"
        },
        new Note
        {
            PatientId = 3,
            Title = "",
            Body = "Le patient déclare qu'il fume depuis peu"
        },
        new Note
        {
            PatientId = 3,
            Title = "",
            Body = "Le patient déclare qu'il est fumeur et qu'il a cessé de fumer l'année dernière Il se plaint également de crises d’apnée respiratoire anormales Tests de laboratoire indiquant un taux de cholestérol LDL élevé"
        },
        new Note
        {
            PatientId = 4,
            Title = "",
            Body = "Le patient déclare qu'il lui est devenu difficile de monter les escaliers Il se plaint également d’être essoufflé Tests de laboratoire indiquant que les anticorps sont élevés Réaction aux médicaments"
        },
        new Note
        {
            PatientId = 4,
            Title = "",
            Body = "Le patient déclare qu'il a mal au dos lorsqu'il reste assis pendant longtemps"
        },
        new Note
        {
            PatientId = 4,
            Title = "",
            Body = "Le patient déclare avoir commencé à fumer depuis peu Hémoglobine A1C supérieure au niveau recommandé"
        },
        new Note
        {
            PatientId = 4,
            Title = "",
            Body = "Taille, Poids, Cholestérol, Vertige et Réaction"
        }
    };

    public async Task SeedNotesAsync()
    {
        List<Note> notes = notesAtStartup;
        int batchSize = 10;
        List<Task> tasks = new();

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