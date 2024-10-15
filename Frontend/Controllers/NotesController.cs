using Microsoft.AspNetCore.Mvc;
using Frontend.Controllers.Service;
using Frontend.Models;
using System.Net;


namespace Frontend.Controllers;

[Route("note")]
public class NotesController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly HttpClientService _httpClientService;
    private readonly ILogger<NotesController> _logger;
    private readonly string _noteServiceUrl;

    private readonly PatientService _patientService;

    public NotesController(ILogger<NotesController> logger, HttpClient httpClient,
     HttpClientService httpClientService,
    IConfiguration configuration, PatientService patientService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _patientService = patientService;
        _httpClientService = httpClientService;
        _noteServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("Note");
    }

    [HttpGet("Index")]
    public async Task<IActionResult> Index()
    {
        HttpResponseMessage response = await _httpClient.GetAsync(_noteServiceUrl);
        if (response.IsSuccessStatusCode)
        {
            List<Frontend.Models.Note>? notes = await response.Content.ReadFromJsonAsync<List<Frontend.Models.Note>>();
            if (notes != null)
            {
                foreach (Frontend.Models.Note note in notes)
                {
                    Console.WriteLine($"Notes: {note.Id} {note.PatientId} {note.Title} by {note.PractionnerId}");
                }
            }

            return View(notes);
        }

        ModelState.AddModelError(string.Empty, "Unable to load notes.");
        TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
        return View(new List<Frontend.Models.Note>());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        HttpResponseMessage responseFromNoteService = await _httpClient.GetAsync($"{_noteServiceUrl}/notes/{Uri.EscapeDataString(id.ToString())}");

        if (responseFromNoteService.IsSuccessStatusCode)
        {

            Frontend.Models.Note? note = await responseFromNoteService.Content.ReadFromJsonAsync<Frontend.Models.Note>();

            if (note != null)
            {
                return View(note);
            }

            return NotFound("Note not found.");
        }

        ModelState.AddModelError(string.Empty, "Note not found.");
        TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
        return View();
    }

    [HttpGet("create")]
    public IActionResult Create(int patientId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        ViewBag.PatientId = patientId;
        return View();
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(Frontend.Models.Note note)
    {
        // NOW: Add needed information to the post request as practionnerId, patientid, createddate, lastupdateddate, title, body
        // DONE: Replace with the actual practionnerId
        // FUTURE: Should be impossible but GetUserIdFromAuthToken is null management should be done otherwise
        note.PractionnerId = await _patientService.GetUserIdFromAuthToken() ?? "11a6aca3-618d-472a-82a7-db9b90f7e56f";

        note.CreatedDate = DateTime.Now;
        note.LastUpdatedDate = DateTime.Now;

        if (ModelState.IsValid)
        {
            HttpRequestMessage request = new(HttpMethod.Post, $"{_noteServiceUrl}/")
            {
                Content = JsonContent.Create(note)
            };
            HttpResponseMessage response = await _httpClientService.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Models.Note? createdNote = await response.Content.ReadFromJsonAsync<Frontend.Models.Note>();
                if (createdNote != null)
                {
                    return RedirectToAction(nameof(Details), new { id = createdNote.Id });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Failed to create note.");
                    _logger.LogError("Failed to create note.");
                    TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                    return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Error from the server");
                _logger.LogError("Error from the server : {ReasonPhrase}", response.ReasonPhrase);
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
            }
        }
        else
        {
            _logger.LogError("Model state is not valid.");
            ModelState.AddModelError(string.Empty, "Unable to create note.");
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            return View(note);
        }
    }

    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        if (ModelState.IsValid)
        {
            HttpRequestMessage request = new(HttpMethod.Get, $"{_noteServiceUrl}/notes/{Uri.EscapeDataString(id.ToString())}");
            HttpResponseMessage response = await _httpClientService.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Frontend.Models.Note? note = await response.Content.ReadFromJsonAsync<Frontend.Models.Note>();
                return View(note);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ModelState.AddModelError(string.Empty, "Note not found.");
                _logger.LogError("Note not found.");
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                return View();
            }
            ModelState.AddModelError(string.Empty, "Unable to load note for edit.");
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            return View();
        }
        else
        {
            _logger.LogError("Model state is not valid.");
            ModelState.AddModelError(string.Empty, "Unable to load note for edit.");
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            return View();
        }
    }

    [HttpPost("edit/{id}")]
    public async Task<IActionResult> Edit(Frontend.Models.Note note)
    {
        if (ModelState.IsValid)
        {
            _logger.LogInformation("Updating note with id {Id} to {NoteTitle}", note.Id, note.Title);

            UriBuilder uriBuilder = new(_noteServiceUrl)
            {
                Path = $"notes/{note.Id}"
            };

            HttpRequestMessage request = new(HttpMethod.Put, uriBuilder.Uri)
            {
                Content = JsonContent.Create(note)
            };
            HttpResponseMessage response = await _httpClientService.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Note with id {PatientId} was successfully updated.", note.Id);
                return RedirectToAction(nameof(Details), new { id = note.Id });
            }
            else
            {
                _logger.LogError("Failed to update note with id {PatientId}. Status code: {StatusCode}", note.Id, response.StatusCode);
                await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(response.StatusCode.ToString(), "Unable to update note.");
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                return View(note);
            }
        }
        else
        {
            _logger.LogError("Model state is not valid.");
            ModelState.AddModelError(string.Empty, "Unable to update note.");
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            return View(note);
        }
    }

    [HttpGet("delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        HttpRequestMessage request = new(HttpMethod.Get, $"{_noteServiceUrl}/{Uri.EscapeDataString(id.ToString())}");
        HttpResponseMessage response = await _httpClientService.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            Frontend.Models.Note? note = await response.Content.ReadFromJsonAsync<Frontend.Models.Note>();
            return View(note);
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to load note with id {PatientId}. Status code: {StatusCode}, Error: {Error}", id, response.StatusCode, errorContent);
            ModelState.AddModelError(response.StatusCode.ToString(), "Unable to load note for deletion.");
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
    }

    [HttpPost("delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        HttpRequestMessage request = new(HttpMethod.Delete, $"{_noteServiceUrl}/notes/{Uri.EscapeDataString(id.ToString())}");
        HttpResponseMessage response = await _httpClientService.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to delete note with id {PatientId}. Status code: {StatusCode}, Error: {Error}", id, response.StatusCode, errorContent);
            ModelState.AddModelError(string.Empty, "Unable to delete note.");
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
    }

    [HttpGet("patient/{patientId}")]
    public async Task<IActionResult> GetNotesFromPatientId(int patientId)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        HttpRequestMessage request = new(HttpMethod.Get, $"{_noteServiceUrl}/notes/patient/{Uri.EscapeDataString(patientId.ToString())}");
        HttpResponseMessage response = await _httpClientService.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction(nameof(AuthController.Login), nameof(AuthController).Replace("Controller", ""));
        }

        if (response.IsSuccessStatusCode)
        {
            List<Frontend.Models.Note>? notes = await response.Content.ReadFromJsonAsync<List<Frontend.Models.Note>>();
            return View(notes);
        }

        else
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to load notes for patient with id {PatientId}. Status code: {StatusCode}, Error: {Error}", patientId, response.StatusCode, errorContent);
            ModelState.AddModelError(string.Empty, "Unable to load notes for patient.");
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
    }

}

