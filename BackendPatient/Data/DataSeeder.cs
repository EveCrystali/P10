using BackendPatient.Models;
namespace BackendPatient.Data;

public class DataSeeder(ApplicationDbContext dbContext)
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public void SeedPatients()
    {
        // Tableau des patients non formatés
        (string nom, string prenom, string dateDeNaissance, string genre, string? adresse, string? telephone)[] patientsUnformatted =
        {
            ("TestNone", "Test", "1966-12-31", "F", "1 Brookside St", "100-222-3333"),
            ("TestBorderline", "Test", "1945-06-24", "M", "2 High St", "200-333-4444"),
            ("TestInDanger", "Test", "2004-06-18", "M", "3 Club Road", "300-444-5555"),
            ("TestEarlyOnset", "Test", "2002-06-28", "F", "4 Valley Dr", "400-555-6666")
        };

        // Formater les patients avant insertion
        List<Patient> patientsFormatted = patientsUnformatted
                                          .Select(Patient.FormatPatient)
                                          .ToList();

        // Charger les patients existants en mémoire pour éviter les doublons
        var existingPatients = _dbContext.Patients
                                         .Select(p => new
                                         {
                                             p.FirstName,
                                             p.LastName,
                                             p.DateOfBirth
                                         })
                                         .ToList();

        int batchSize = 10;
        int counter = 0;

        foreach (Patient patient in patientsFormatted)
        {
            // Avoid duplicate patients
            if (existingPatients.Exists(p => p.FirstName == patient.FirstName &&
                                             p.LastName == patient.LastName &&
                                             p.DateOfBirth == patient.DateOfBirth))
            {
                Console.WriteLine($"Skipping duplicate patient: {patient.FirstName} {patient.LastName} {patient.DateOfBirth}");
                continue;
            }

            _dbContext.Patients.Add(patient);
            counter++;

            // Save every 10 patients (batch size)
            if (counter % batchSize == 0)
            {
                _dbContext.SaveChanges();
                Console.WriteLine($"Saved {counter} patients so far...");
            }
        }

        // Save any remaining patients
        if (counter % batchSize != 0)
        {
            _dbContext.SaveChanges();
            Console.WriteLine($"Final save, total patients saved: {counter}");
        }
    }
}