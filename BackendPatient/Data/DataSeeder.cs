using System;
using System.Globalization;
using BackendPatient.Models;

namespace BackendPatient.Data;

public class DataSeeder(ApplicationDbContext dbContext)
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public void SeedPatients()
    {
        (string nom, string prenom, string? dateDeNaissance, string? genre, string? adresse, string? telephone)[] patientsUnformatted =
        [
            (nom: "TestNone", prenom: "Test", dateDeNaissance: "1966-12-31", genre: "F", adresse: "1 Brookside St", telephone: "100-222-3333"),
            (nom: "TestBorderline", prenom: "Test", dateDeNaissance: "1945-06-24", genre: "M", adresse: "2 High St", telephone: "200-333-4444"),
            (nom: "TestInDanger", prenom: "Test", dateDeNaissance: "2004-06-18", genre: "M", adresse: "3 Club Road", telephone: "300-444-5555"),
            (nom: "TestEarlyOnset", prenom: "Test", dateDeNaissance: "2002-06-28", genre: "F", adresse: "4 Valley Dr", telephone: "400-555-6666"),
        ];

        (string nom, string prenom, DateTime? dateDeNaissance, string? genre, string? adresse, string? telephone)[] patientsFormatted = new (string, string, DateTime?, string?, string?, string?)[patientsUnformatted.Length];

        for (int i = 0; i < patientsUnformatted.Length; i++)
        {
            patientsFormatted[i] = (patientsUnformatted[i].nom, patientsUnformatted[i].prenom, null, patientsUnformatted[i].genre, patientsUnformatted[i].adresse, patientsUnformatted[i].telephone);
            try
            {
                patientsFormatted[i].dateDeNaissance = DateTime.ParseExact(patientsUnformatted[i].dateDeNaissance ?? string.Empty, "yyyy-MM-dd", CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception occured while parsing date when seeding patients: " + ex.Message);
                patientsFormatted[i].dateDeNaissance = null;
            }
        }

        int batchSize = 10;
        int counter = 0;


        for (int i = 0; i < patientsFormatted.Length; i++)
        {
            Patient patient = new()
            {
                FirstName = patientsFormatted[i].nom,
                LastName = patientsFormatted[i].prenom,
                DateOfBirth = patientsFormatted[i].dateDeNaissance,
                Gender = patientsFormatted[i].genre,
                Address = patientsFormatted[i].adresse,
                PhoneNumber = patientsFormatted[i].telephone
            };

            if (_dbContext.Patients.FirstOrDefault(p => p.FirstName == patient.FirstName && p.LastName == patient.LastName && p.DateOfBirth == patient.DateOfBirth) != null)
            {
                Console.WriteLine("Skipping duplicate patient: " + patient.FirstName + " " + patient.LastName + " " + patient.DateOfBirth);
                continue;
            }

            _dbContext.Patients.Add(patient);

            counter++;
            if (counter % batchSize == 0)
            {
                _dbContext.SaveChanges();
                Console.WriteLine("Saved " + counter + " patients");
            }

        }

        _dbContext.SaveChanges();
    }

}
