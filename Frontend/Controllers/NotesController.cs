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

        await ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to load notes", modelErrorMessage: "Unable to load notes.", response: response);
        return View(new List<Frontend.Models.Note>());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(string id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        HttpResponseMessage responseFromNoteService = await _httpClient.GetAsync($"{_noteServiceUrl}/{Uri.EscapeDataString(id)}");

        if (responseFromNoteService.IsSuccessStatusCode)
        {

            Frontend.Models.Note? note = await responseFromNoteService.Content.ReadFromJsonAsync<Frontend.Models.Note>();

            if (note != null)
            {
                return View(note);
            }

            return NotFound("Note not found.");
        }
        
        await ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to load note", modelErrorMessage: "Unable to load note.", response: responseFromNoteService, id: id);
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
        // DONE: Add needed information to the post request as practionnerId, createddate, lastupdateddate
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
                    await ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to create note", modelErrorMessage: "Unable to create note.", response: response);
                    return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
                }
            }
            else
            {
                await ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to create note - Error from the server", modelErrorMessage: "Unable to create note.", response: response);
                return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
            }
        }
        else
        {
            await ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to create note", modelErrorMessage: "odel state is not valid.", response: null);
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
                await ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Note not found", modelErrorMessage: "Note not found.", response: response, id: id.ToString());
                return View();
            }
            await ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to load note for edit", modelErrorMessage: "Unable to load note for edit.", response: response, id: id);
            return View();
        }
        else
        {
            await ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Model state is not valid", modelErrorMessage: "Model state is not valid.", response: null, id: id);
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
                Path = $"{note.Id}"
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
                await ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to update note", modelErrorMessage: "Unable to update note.", response: response, id: note.Id);
                return View(note);
            }
        }
        else
        {
            await ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to update note", modelErrorMessage: "Unable to update note.", response: null, id: note.Id);
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
            await ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: $"Failed to load note with id {id}. Status code: {response.StatusCode}", modelErrorMessage: "Unable to load note for deletion.", response: response, id: id);
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
            await ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: $"Failed to delete note with id {id}. Status code: {response.StatusCode}", modelErrorMessage: "Unable to delete note.", response: response, id: id);
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
            await ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: $"Failed to load notes for patient with id {patientId}. Status code: {response.StatusCode}", modelErrorMessage: "Unable to load notes for patient.", response: response, id: patientId.ToString());
            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
    }

}

