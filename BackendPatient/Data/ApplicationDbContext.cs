using System;
using Microsoft.EntityFrameworkCore;
using Patient.Models;

namespace Patient.Data;

public class ApplicationDbContext : DbContext
{

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }

    public DbSet<Patient.Models.Patient> Patients { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // TODO : add custom model configurations

        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient.Models.Patient>().HasKey(p => p.Id);
    }

}
