using BackendPatient.Data;
using BackendPatient.Models;
using BackendPatient.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace BackendPatient.Controllers;

[ApiController]
[Route("patient")]
public class PatientsController(ApplicationDbContext dbContext, IUpdateService<Patient> updateService) : ControllerBase
{

    /// <summary>
    ///     Retrieves a <see cref="Patient" /> from the database by its identifier.
    /// </summary>
    /// <param name="id">The identifier of the patient to retrieve.</param>
    /// <returns>The retrieved <see cref="Patient" /> object, or a 404 Not Found response if no such patient exists.</returns>
    [HttpGet("{id}")]
    [Authorize(Policy = "RequirePractitionerRoleOrHigher")]
    public async Task<ActionResult<Patient>> GetPatient(int id)
    {
        Patient? patient = await dbContext.Patients.FindAsync(id);

        if (patient == null)
        {
            return NotFound("Patient not found");
        }

        return Ok(patient);
    }

    [HttpGet("dto/{id}")]
    public async Task<ActionResult<Patient>> GetPatientDtoDiabetesRiskPrediction(int id)
    {
        Patient? patient = await dbContext.Patients.FindAsync(id);

        if (patient == null)
        {
            return NotFound("Patient not found");
        }

        PatientDtoDiabetesRiskPrediction patientDtoDiabetesRiskPrediction = new()
        {
            Id = patient.Id,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender
        };

        return Ok(patientDtoDiabetesRiskPrediction);
    }

    /// <summary>
    ///     Retrieves a list of all patients from the database.
    /// </summary>
    /// <returns>A list of <see cref="Patient" /> objects.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Patient>>> GetPatients()
    {
        List<Patient> patients = await dbContext.Patients.ToListAsync();

        return Ok(patients);
    }

    /// <summary>
    ///     Updates a <see cref="Patient" /> in the database.
    /// </summary>
    /// <param name="id">The id of the patient to update.</param>
    /// <param name="patient">The updated patient.</param>
    /// <returns>
    ///     A <see cref="Task" /> representing the result of the asynchronous operation. The task result contains the HTTP
    ///     response.
    /// </returns>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequirePractitionerRoleOrHigher")]
    public async Task<IActionResult> PutPatient(int id, [FromBody] Patient patient)
    {
        return await updateService.UpdateEntity(id, patient, PatientExists, getIdFunc: p => p.Id);
    }

    /// <summary>
    ///     Creates a new <see cref="Patient" /> in the database.
    /// </summary>
    /// <param name="patient">The new patient to be created.</param>
    /// <returns>
    ///     A <see cref="Task" /> representing the result of the asynchronous operation. The task result contains the HTTP
    ///     response.
    /// </returns>
    [HttpPost]
    [Authorize(Policy = "RequirePractitionerRoleOrHigher")]
    public async Task<ActionResult<Patient>> PostPatient([FromBody] Patient patient)
    {
        // Validate the patient before adding it to the database
        try
        {
            patient.Validate();
        }
        catch (Exception ex)
        {
            // Return a bad request if the patient is not valid
            return BadRequest(ex.Message);
        }

        // patient.Id must be set by the database automatically, so we set it to 0 to force it
        // patient.Id = 0;

        // Add the patient to the database
        dbContext.Patients.Add(patient);
        await dbContext.SaveChangesAsync();

        // Return a 201 Created response with the newly created patient
        return CreatedAtAction(nameof(GetPatient), new
        {
            id = patient.Id
        }, patient);
    }

    /// <summary>
    ///     Deletes a <see cref="Patient" /> from the database.
    /// </summary>
    /// <param name="id">The id of the patient to delete.</param>
    /// <returns>
    ///     A <see cref="Task" /> representing the result of the asynchronous operation. The task result contains the HTTP
    ///     response.
    /// </returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequirePractitionerRoleOrHigher")]
    public async Task<IActionResult> DeletePatient(int id)
    {
        // Find the patient in the database
        Patient? patient = await dbContext.Patients.FindAsync(id);
        if (patient == null)
        {
            // Return a 404 Not Found response if the patient is not found
            return NotFound("Patient not found");
        }

        // Remove the patient from the database
        dbContext.Patients.Remove(patient);
        await dbContext.SaveChangesAsync();

        // Return a 200 OK response with a message indicating that the patient was deleted
        return Ok("Patient deleted");
    }

    private bool PatientExists(Patient patient)
    {
        return dbContext.Patients.Any(e => e.Id == patient.Id);
    }
}