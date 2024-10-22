using System.Globalization;
using System.Net;
using Frontend.Controllers.Service;
using Microsoft.AspNetCore.Mvc;

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
        HttpRequestMessage request = new(HttpMethod.Get, $"{_noteServiceUrl}");
        HttpResponseMessage response = await _httpClientService.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            List<Frontend.Models.Note>? notes = await response.Content.ReadFromJsonAsync<List<Frontend.Models.Note>>();
            if (notes != null)
            {
                foreach (Frontend.Models.Note note in notes)
                {
                    Console.WriteLine($"Notes: {note.Id} {note.PatientId} {note.Title} by {note.Creator}");
                }
            }

            return View(notes);
        }

        ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to load notes", modelErrorMessage: "Unable to load notes.", response: response);
        return View(new List<Frontend.Models.Note>());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(string id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        HttpRequestMessage request = new(HttpMethod.Get, $"{_noteServiceUrl}/{Uri.EscapeDataString(id)}");
        HttpResponseMessage responseFromNoteService = await _httpClientService.SendAsync(request);

        if (responseFromNoteService.IsSuccessStatusCode)
        {
            Frontend.Models.Note? note = await responseFromNoteService.Content.ReadFromJsonAsync<Frontend.Models.Note>();

            if (note != null)
            {
                return View(note);
            }

            return NotFound("Note not found.");
        }

        ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to load note", modelErrorMessage: "Unable to load note.", response: responseFromNoteService);
        return View();
    }

    [HttpGet("create")]
    public IActionResult Create(int patientId, string lastName, string firstName)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        ViewBag.PatientId = patientId;
        ViewBag.LastName = lastName;
        ViewBag.FirstName = firstName;

        return View();
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(Frontend.Models.Note note)
    {
        note.Creator = await _patientService.GetUsernameFromAuthToken();

        DateTime nowFormatted = DateTime.ParseExact(DateTime.Now.ToString("yyyy-MM-dd HH:mm"), "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        note.CreatedDate = nowFormatted;
        note.LastUpdatedDate = nowFormatted;

        if (ModelState.IsValid)
        {
            HttpRequestMessage request = new(HttpMethod.Post, $"{_noteServiceUrl}/")
            {
                Content = JsonContent.Create(note)
            };
            HttpResponseMessage response = await _httpClientService.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return RedirectToAction(nameof(AuthController.Login), nameof(AuthController).Replace("Controller", ""));
            }

            if (response.IsSuccessStatusCode)
            {
                Models.Note? createdNote = await response.Content.ReadFromJsonAsync<Frontend.Models.Note>();
                if (createdNote != null)
                {
                    return RedirectToAction(nameof(Details), new { id = createdNote.Id });
                }
                else
                {
                    ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to create note", modelErrorMessage: "Unable to create note.", response: response);
                    return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
                }
            }
            else
            {
                ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to create note - Error from the server", modelErrorMessage: "Unable to create note.", response: response);
                return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
            }
        }
        else
        {
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to create note", modelErrorMessage: "odel state is not valid.", response: null);
            return View(note);
        }
    }

    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(string id)
    {
        if (ModelState.IsValid)
        {
            HttpRequestMessage request = new(HttpMethod.Get, $"{_noteServiceUrl}/{Uri.EscapeDataString(id)}");
            HttpResponseMessage response = await _httpClientService.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Frontend.Models.Note? note = await response.Content.ReadFromJsonAsync<Frontend.Models.Note>();
                return View(note);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Note not found", modelErrorMessage: "Note not found.", response: response);
                return View();
            }
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to load note for edit", modelErrorMessage: "Unable to load note for edit.", response: response);
            return View();
        }
        else
        {
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Model state is not valid", modelErrorMessage: "Model state is not valid.");
            return View();
        }
    }

    [HttpPost("edit/{id}")]
    public async Task<IActionResult> Edit(Frontend.Models.Note note)
    {
        note.LastUpdatedDate = DateTime.ParseExact(DateTime.Now.ToString("yyyy-MM-dd HH:mm"), "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        if (ModelState.IsValid)
        {
            _logger.LogInformation("Updating note with id {Id} to {NoteTitle}", note.Id, note.Title);

            HttpRequestMessage request = new(HttpMethod.Put, $"{_noteServiceUrl}/{Uri.EscapeDataString(note.Id)}")
            {
                Content = JsonContent.Create(note)
            };
            HttpResponseMessage response = await _httpClientService.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return RedirectToAction(nameof(AuthController.Login), nameof(AuthController).Replace("Controller", ""));
            }
            else if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Note with id {PatientId} was successfully updated.", note.Id);
                return RedirectToAction(nameof(Details), nameof(NotesController).Replace("Controller", ""), new { id = note.Id });
            }
            else
            {
                ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to update note", modelErrorMessage: "Unable to update note.", response: response);
                return View(note);
            }
        }
        else
        {
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to update note", modelErrorMessage: "Unable to update note.");
            return View(note);
        }
    }

    [HttpGet("delete/{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        HttpRequestMessage request = new(HttpMethod.Get, $"{_noteServiceUrl}/{Uri.EscapeDataString(id)}");
        HttpResponseMessage response = await _httpClientService.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            Frontend.Models.Note? note = await response.Content.ReadFromJsonAsync<Frontend.Models.Note>();
            return View(note);
        }
        else
        {
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: $"Failed to load note. Status code: {response.StatusCode}", modelErrorMessage: "Unable to load note for deletion.", response: response);
            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
    }

    [HttpPost("delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        HttpRequestMessage request = new(HttpMethod.Delete, $"{_noteServiceUrl}/{Uri.EscapeDataString(id)}");
        HttpResponseMessage response = await _httpClientService.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
        else
        {
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: $"Failed to delete note. Status code: {response.StatusCode}", modelErrorMessage: "Unable to delete note.", response: response);
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

        HttpRequestMessage request = new(HttpMethod.Get, $"{_noteServiceUrl}/patient/{Uri.EscapeDataString(patientId.ToString())}");
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
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: $"Failed to load notes for patient with id {patientId}. Status code: {response.StatusCode}", modelErrorMessage: "Unable to load notes for patient.", response: response);
            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
    }
}