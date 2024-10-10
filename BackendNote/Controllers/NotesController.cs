using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BackendNote.Models;
using BackendNote.Services;

namespace BackendNote.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotesController : ControllerBase
    {
        private readonly NotesService _notesService;

        public NotesController(NotesService notesService) =>
            _notesService = notesService;

        [HttpGet]
        public async Task<List<Note>> Get() =>
            await _notesService.GetAsync();

        [HttpGet("{id:length(24)}")]
        public async Task<ActionResult<Note>> Get(string id)
        {
            var note = await _notesService.GetAsync(id);

            if (note is null)
            {
                return NotFound();
            }

            return note;
        }

        [HttpPost]
        public async Task<IActionResult> Post(Note newnote)
        {
            await _notesService.CreateAsync(newnote);

            return CreatedAtAction(nameof(Get), new { id = newnote.Id }, newnote);
        }

        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, Note updatednote)
        {
            var note = await _notesService.GetAsync(id);

            if (note is null)
            {
                return NotFound();
            }

            updatednote.Id = note.Id;

            await _notesService.UpdateAsync(id, updatednote);

            return NoContent();
        }

        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            var note = await _notesService.GetAsync(id);

            if (note is null)
            {
                return NotFound();
            }

            await _notesService.RemoveAsync(id);

            return NoContent();

        }
    }
}
