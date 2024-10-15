using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BackendNote.Models;
using BackendNote.Services;
using Microsoft.AspNetCore.Authorization;

namespace BackendNote.Controllers
{
    [Route("note")]
    [ApiController]
    // BUG : Handle authorization 
    // The app is not able to even launch (frontend) when Authorize is used here
    // [Authorize(Policy = "RequirePractitionerRoleOrHigher")]
    public class NotesController : ControllerBase
    {
        private readonly NotesService _notesService;

        public NotesController(NotesService notesService) =>
            _notesService = notesService;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Note>>> Get()
        {
            List<Note> notes = await _notesService.GetAsync();

            return notes != null ? Ok(notes) : NotFound("No notes found");

        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Note>> GetNote(string id)
        {
            Note? note = await _notesService.GetAsync(id);

            if (note is null)
            {
                return NotFound("Note not found");
            }

            return Ok(note);
        }

        [HttpGet("patient/{patientId}")]
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

            newNote.CreatedDate = DateTime.UtcNow; 

            await _notesService.CreateAsync(newNote);

            return CreatedAtAction(nameof(Get), new { id = newNote.Id }, newNote);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNote(string id, Note updatedNote)
        {
            var note = await _notesService.GetAsync(id);

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
            updatedNote.LastUpdatedDate = DateTime.UtcNow; 

            await _notesService.UpdateAsync(id, updatedNote);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNote(string id)
        {
            var note = await _notesService.GetAsync(id);

            if (note is null)
            {
                return NotFound("Note not found");
            }

            await _notesService.RemoveAsync(id);

            return Ok("Note deleted");

        }
    }
}
