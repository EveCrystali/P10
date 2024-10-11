using System;
using Frontend.Models;

namespace Frontend.Controllers.Service;

public class PatientService
{
    public PatientNotesViewModel MapPatientNoteToPatientNotesViewModel(Patient patient, List<Note> notes)
    {
        PatientNotesViewModel patientNotesViewModel = new ()
        {
            PatientId = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Address = patient.Address,
            PhoneNumber = patient.PhoneNumber,
            Notes = notes
        };
        return patientNotesViewModel;
        
    }
}