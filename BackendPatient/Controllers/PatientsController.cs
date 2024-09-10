using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BackendPatient.Data;
using BackendPatient.Models;
using BackendPatient.Services;

namespace BackendPatient.Controllers;

[ApiController]
[Route("api/patient")]
public class PatientsController(ApplicationDbContext dbContext, IUpdateService<Patient> updateService) : ControllerBase
{
    private readonly ApplicationDbContext _dbContext = dbContext;
    private readonly IUpdateService<Patient> _updateService = updateService;


    [HttpGet("{id}")]
    public async Task<ActionResult<Patient>> GetPatient(int id)
    {
        Patient? patient = await _dbContext.Patients.FindAsync(id);

        if (patient == null)
        {
            return NotFound("Patient not found");
        }

        return Ok(patient);
    }

    [HttpGet]
    public async Task<ActionResult<Patient>> GetPatients()
    {
        List<Patient> patients = await _dbContext.Patients.ToListAsync();
        return patients != null ? Ok(patients) : BadRequest("No patients found");
    }

    [HttpPut("{id}")]
    // FIXME: Unable to resolve service for type 'BackendPatient.Data.ApplicationDbContext' while attempting to activate 'BackendPatient.Controllers.PatientsController'.
    public async Task<IActionResult> PutPatient(int id, Patient patient)
    {
        return await _updateService.UpdateEntity(id, patient, PatientExists, p => p.Id);
    }

    [HttpPost]
    public async Task<ActionResult<Patient>> PostPatient(Patient patient)
    {
        try
        {
            patient.Validate();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        // patient.Id must be set by the database automatically, so we set it to 0 to force it
        patient.Id = 0;

        _dbContext.Patients.Add(patient);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPatient), new { id = patient.Id }, patient);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeletePatient(int id)
    {
        var patient = await _dbContext.Patients.FindAsync(id);
        if (patient == null)
        {
            return NotFound("Patient not found");
        }

        _dbContext.Patients.Remove(patient);
        await _dbContext.SaveChangesAsync();

        return Ok("Patient deleted");
    }

    private bool PatientExists(Patient patient)
    {
        return _dbContext.Patients.Any(e => e.Id == patient.Id);
    }
}
