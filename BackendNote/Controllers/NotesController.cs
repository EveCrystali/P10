using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BackendNote.Models;
using BackendNote.Services;

namespace BackendNote.Controllers
{
    [Route("note")]
    [ApiController]
    public class NotesController : ControllerBase
    {
        private readonly NotesService _notesService;

        public NotesController(NotesService notesService) =>
            _notesService = notesService;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Note>>> Get()
        {
            List<Note> note = await _notesService.GetAsync();

            return note != null ? Ok(note) : NotFound("No notes found");

        }

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Note>> Get(string id)
        {
            Note? note = await _notesService.GetAsync(id);

            if (note is null)
            {
                return NotFound("Note not found");
            }

            return Ok(note);
        }

        [HttpGet("user/{userId:length(24)}")]
        public async Task<ActionResult<Note>> GetFromUserId(string userId)
        {
            List<Note>? notes = await _notesService.GetFromUserIdAsync(userId);

            if (notes is null)
            {
                return NotFound("Note not found");
            }

            return Ok(notes);
        }

        [HttpPost]
        public async Task<IActionResult> Post(Note newNotes)
        {
            try
            {
                newNotes.Validate();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            await _notesService.CreateAsync(newNotes);

            return CreatedAtAction(nameof(Get), new { id = newNotes.Id }, newNotes);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, Note updatednote)
        {
            var note = await _notesService.GetAsync(id);

            if (note is null)
            {
                return NotFound("Note not found");
            }

            try
            {
                updatednote.Validate();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

            updatednote.Id = note.Id;

            await _notesService.UpdateAsync(id, updatednote);

            ////   Ok() or NoContent() ? Is it necessary to return the updated note?
            // NOTE: Generally, NoContent() should be used for PUT requests specially when
            // the content of the view is not changed (like when we want to save without changing of view)
            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
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
