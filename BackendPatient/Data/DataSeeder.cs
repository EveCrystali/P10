using BackendPatient.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
namespace BackendPatient.Data;

public class DataSeeder(ApplicationDbContext dbContext)
{
    private readonly ApplicationDbContext _dbContext = dbContext;

    public void SeedPatients()
    {
        // Tableau des patients non formatés
        (int Id, string nom, string prenom, string dateDeNaissance, string genre, string? adresse, string? telephone)[] patientsUnformatted =
        {
            (1, "TestNone", "Test", "1966-12-31", "F", "1 Brookside St", "100-222-3333"),
            (2, "TestBorderline", "Test", "1945-06-24", "M", "2 High St", "200-333-4444"),
            (3, "TestInDanger", "Test", "2004-06-18", "M", "3 Club Road", "300-444-5555"),
            (4, "TestEarlyOnset", "Test", "2002-06-28", "F", "4 Valley Dr", "400-555-6666")
        };

        // Formater les patients avant insertion
        List<Patient> patientsFormatted = patientsUnformatted
                                          .Select(Patient.FormatPatient)
                                          .ToList();

        using IDbContextTransaction transaction = _dbContext.Database.BeginTransaction();
        try
        {
            SetIdentityInsert("Patients");

            foreach (Patient patient in patientsFormatted)
            {

                // Vérifier si le patient avec cet ID existe déjà
                Patient? existingPatient = _dbContext.Patients.FirstOrDefault(p => p.Id == patient.Id);

                if (existingPatient != null)
                {
                    // Réinitialiser les données du patient existant
                    existingPatient.FirstName = patient.FirstName;
                    existingPatient.LastName = patient.LastName;
                    existingPatient.DateOfBirth = patient.DateOfBirth;
                    existingPatient.Gender = patient.Gender;
                    existingPatient.Address = patient.Address;
                    existingPatient.PhoneNumber = patient.PhoneNumber;
                    _dbContext.Patients.Update(existingPatient);
                    Console.WriteLine($"Updated test patient with ID {patient.Id}");
                }
                else
                {
                    // Ajouter le patient avec son ID spécifique
                    _dbContext.Patients.Add(patient);
                    Console.WriteLine($"Added new test patient with ID {patient.Id}");
                }
            }

            _dbContext.SaveChanges();
            transaction.Commit();
            Console.WriteLine("Test patients seeded successfully.");
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
        finally
        {
            // Désactiver IDENTITY_INSERT une fois terminé
            SetIdentityInsert("Patients", false);
        }

    }

    private void SetIdentityInsert(string tableName, bool enable = true)
    {
        string sql = enable
            ? $"SET IDENTITY_INSERT {tableName} ON;"
            : $"SET IDENTITY_INSERT {tableName} OFF;";
        _dbContext.Database.ExecuteSqlRaw(sql);
    }
}