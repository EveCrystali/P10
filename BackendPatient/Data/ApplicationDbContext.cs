using System;
using Microsoft.EntityFrameworkCore;
using BackendPatient.Models;

namespace BackendPatient.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Patient> Patients { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Patient>().HasKey(p => p.Id);
    }

}
