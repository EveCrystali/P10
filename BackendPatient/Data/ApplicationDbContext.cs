using BackendPatient.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendPatient.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>().HasKey(p => p.Id);
        modelBuilder.Entity<Patient>()
            .Property(p => p.Id)
            .ValueGeneratedOnAdd();
    }
}