using System;

namespace Frontend.Models;

public class PatientNotesViewModel
{
    public required int PatientId { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public required DateOnly DateOfBirth { get; set; }

    public required string Gender { get; set; }

    public string? Address { get; set; }

    public string? PhoneNumber { get; set; }

    public virtual List<Note>? Notes { get; set; }

}
