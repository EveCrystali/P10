using System.Globalization;
using BackendNote.Models;
using BackendNote.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BackendNote.Controllers;

[Route("note")]
[ApiController]
public class NotesController : ControllerBase
{
    private readonly NotesService _notesService;

    public NotesController(NotesService notesService)
    {
        _notesService = notesService;
    }

    [HttpGet]
    [Authorize(Policy = "RequirePractitionerRoleOrHigher")]
    public async Task<ActionResult<IEnumerable<Note>>> Get()
    {
        List<Note> notes = await _notesService.GetAsync();

        return notes != null ? Ok(notes) : NotFound("No notes found");
    }


    [HttpGet("{id}")]
    [Authorize(Policy = "RequirePractitionerRoleOrHigher")]
    public async Task<ActionResult<Note>> GetNote(string id)
    {
        Note? note = await _notesService.GetAsync(id);

        if (note is null)
        {
            return NotFound("Note not found");
        }

        return Ok(note);
    }


    [HttpGet("dto/{id}")]
    [Authorize(Policy = "RequirePractitionerRoleOrHigher")]
    public async Task<ActionResult<Note>> GetNoteDTODiabetesRiskPrediction(string id)
    {
        Note? note = await _notesService.GetAsync(id);

        if (note is null)
        {
            return NotFound("Note not found");
        }

        NoteDtoDiabetesRiskPrediction noteDtoDiabetesRiskPrediction = new()
        {
            Id = note.Id,
            PatientId = note.PatientId,
            Title = note.Title,
            Body = note.Body
        };

        return Ok(noteDtoDiabetesRiskPrediction);
    }

    [HttpGet("patient/{patientId}")]
    // [Authorize(Policy = "RequirePractitionerRoleOrHigher")]
    public async Task<ActionResult<Note>> GetNotesFromPatientId(int patientId)
    {
        List<Note>? notes = await _notesService.GetFromPatientIdAsync(patientId);

        if (notes is null)
        {
            return NotFound("Note not found");
        }

        return Ok(notes);
    }

    [HttpPost]
    [Authorize(Policy = "RequirePractitionerRoleOrHigher")]
    public async Task<IActionResult> PostNote(Note newNote)
    {
        try
        {
            newNote.Validate();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        if (newNote.CreatedDate == null)
        {
            newNote.CreatedDate = DateTime.ParseExact(DateTime.Now.ToString("yyyy-MM-dd HH:mm"), "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
            ;
        }

        await _notesService.CreateAsync(newNote);

        return CreatedAtAction(nameof(Get), new
        {
            id = newNote.Id
        }, newNote);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "RequirePractitionerRoleOrHigher")]
    public async Task<IActionResult> UpdateNote(string id, Note updatedNote)
    {
        Note? note = await _notesService.GetAsync(id);

        if (note is null)
        {
            return NotFound("Note not found");
        }

        try
        {
            updatedNote.Validate();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }

        updatedNote.Id = note.Id;

        if (updatedNote.LastUpdatedDate == null)
        {
            updatedNote.LastUpdatedDate = DateTime.ParseExact(DateTime.Now.ToString("yyyy-MM-dd HH:mm"), "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        }

        await _notesService.UpdateAsync(id, updatedNote);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequirePractitionerRoleOrHigher")]
    public async Task<IActionResult> DeleteNote(string id)
    {
        Note? note = await _notesService.GetAsync(id);

        if (note is null)
        {
            return NotFound("Note not found");
        }

        await _notesService.RemoveAsync(id);

        return Ok("Note deleted");
    }
}