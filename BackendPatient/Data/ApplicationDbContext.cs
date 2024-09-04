using System;
using Microsoft.EntityFrameworkCore;
using BackendPatient.Models;

namespace BackendPatient.Data;

public class ApplicationDbContext : DbContext
{

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }

    public DbSet<BackendPatient.Models.Patient> Patients { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO : add custom model configurations

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BackendPatient.Models.Patient>().HasKey(p => p.Id);
    }

}
