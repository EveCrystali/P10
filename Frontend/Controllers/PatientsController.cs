using System.Net;
using Frontend.Controllers.Service;
using Frontend.Models;
using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers;

[Route("patient")]
public class PatientsController : Controller
{
    private readonly HttpClient _httpClient;

    private readonly HttpClientService _httpClientService;
    private readonly ILogger<PatientsController> _logger;
    private readonly string _patientServiceUrl;
    private readonly PatientService _patientService;
    private readonly string _noteServiceUrl;

    public PatientsController(ILogger<PatientsController> logger, HttpClient httpClient,
    HttpClientService httpClientService,
    IConfiguration configuration, PatientService patientService)
    {
        _logger = logger;
        _httpClient = httpClient;

        _httpClientService = httpClientService;
        _patientService = patientService;
        _patientServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("Patient");
        _noteServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("Note");
    }

    public async Task<IActionResult> Index()
    {
        HttpResponseMessage response = await _httpClient.GetAsync(_patientServiceUrl);
        if (response.IsSuccessStatusCode)
        {
            List<Frontend.Models.Patient>? patients = await response.Content.ReadFromJsonAsync<List<Frontend.Models.Patient>>();
            if (patients != null)
            {
                foreach (Frontend.Models.Patient patient in patients)
                {
                    Console.WriteLine($"Patients: {patient.Id} {patient.FirstName} {patient.LastName}");
                }
            }

            return View(patients);
        }

        ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to load patients", modelErrorMessage: "Unable to load patient");

        return View(new List<Frontend.Models.Patient>());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        HttpRequestMessage request1 = new(HttpMethod.Get, $"{_patientServiceUrl}/{id}");
        HttpResponseMessage responseFromPatientService = await _httpClientService.SendAsync(request1);

        int patientId = id;
        
        HttpRequestMessage request2 = new(HttpMethod.Get, $"{_noteServiceUrl}/patient/{patientId}");
        HttpResponseMessage responseFromNoteService = await _httpClientService.SendAsync(request2);

        if (responseFromNoteService.StatusCode == HttpStatusCode.Unauthorized || responseFromPatientService.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction(nameof(AuthController.Login), nameof(AuthController).Replace("Controller", ""));
        }

        if (responseFromPatientService.IsSuccessStatusCode && responseFromNoteService.IsSuccessStatusCode)
        {
            Frontend.Models.Patient? patient = await responseFromPatientService.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
            List<Frontend.Models.Note>? notes = await responseFromNoteService.Content.ReadFromJsonAsync<List<Frontend.Models.Note>>();

            if (patient != null && notes != null)
            {
                PatientNotesViewModel patientWithNotes = _patientService.MapPatientNoteToPatientNotesViewModel(patient, notes);
                return View(patientWithNotes);
            }

            return View(patient);
        }

        ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Patient not found", modelErrorMessage: "Unable to load patient details.");
        return View();
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(Frontend.Models.Patient patient)
    {
        if (ModelState.IsValid)
        {
            HttpRequestMessage request = new(HttpMethod.Post, $"{_patientServiceUrl}/")
            {
                Content = JsonContent.Create(patient)
            };
            HttpResponseMessage response = await _httpClientService.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return RedirectToAction(nameof(AuthController.Login), nameof(AuthController).Replace("Controller", ""));
            }
            else if (response.IsSuccessStatusCode)
            {
                Models.Patient? createdPatient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
                if (createdPatient != null)
                {
                    return RedirectToAction(nameof(Details), new { id = createdPatient.Id });
                }
                else
                {
                    ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to create patient", modelErrorMessage: "Unable to create patient");
                    return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
                }
            }
            else
            {
                ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: $"Error from the server : {response.ReasonPhrase}", modelErrorMessage: "Unable to create patient", response: response);

                return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
            }
        }
        else
        {
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Model state is not valid.", modelErrorMessage: "Unable to create patient");
            return View(patient);
        }
    }

    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        if (ModelState.IsValid)
        {
            HttpRequestMessage request = new(HttpMethod.Get, $"{_patientServiceUrl}/{id}");
            HttpResponseMessage response = await _httpClientService.SendAsync(request);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return RedirectToAction(nameof(AuthController.Login), nameof(AuthController).Replace("Controller", ""));
            }
            else if (response.IsSuccessStatusCode)
            {
                Frontend.Models.Patient? patient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
                return View(patient);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Patient not found.", modelErrorMessage: "Unable to load patient for edit.");
                return View();
            }
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Failed to load patient", modelErrorMessage: "Unable to load patient for edit.");

            return View();
        }
        else
        {
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Model state is not valid.", modelErrorMessage: "Unable to load patient for edit.");
            return View();
        }
    }

    [HttpPost("edit/{id}")]
    public async Task<IActionResult> Edit(Frontend.Models.Patient patient)
    {
        if (ModelState.IsValid)
        {
            _logger.LogInformation("Updating patient with id {PatientId} to {Patient}", patient.Id, patient);

            HttpRequestMessage request = new(HttpMethod.Put, $"{_patientServiceUrl}/{patient.Id}")
            {
                Content = JsonContent.Create(patient)
            };
            HttpResponseMessage response = await _httpClientService.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return RedirectToAction(nameof(AuthController.Login), nameof(AuthController).Replace("Controller", ""));
            }
            else if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Patient with id {PatientId} was successfully updated.", patient.Id);
                return RedirectToAction(nameof(Details), new { id = patient.Id });
            }
            else
            {
                ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: $"Failed to update patient with id {patient.Id}. Status code: {response.StatusCode}", modelErrorMessage: "Unable to update patient");
                return View(patient);
            }
        }
        else
        {
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: "Model state is not valid.", modelErrorMessage: "Unable to update patient");
            return View(patient);
        }
    }

    [HttpGet("delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        HttpRequestMessage request = new(HttpMethod.Get, $"{_patientServiceUrl}/{id}");
        HttpResponseMessage response = await _httpClientService.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction(nameof(AuthController.Login), nameof(AuthController).Replace("Controller", ""));
        }
        else if (response.IsSuccessStatusCode)
        {
            Frontend.Models.Patient? patient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
            return View(patient);
        }
        else
        {
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: $"Failed to load patient with id {id}. Status code: {response.StatusCode}", modelErrorMessage: "Unable to load patient for deletion.", response: response);
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

        HttpRequestMessage request = new(HttpMethod.Delete, $"{_patientServiceUrl}/{id}");
        HttpResponseMessage response = await _httpClientService.SendAsync(request);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction(nameof(AuthController.Login), nameof(AuthController).Replace("Controller", ""));
        }
        else if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
        else
        {
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, logErrorMessage: $"Failed to delete patient with id {id}. Status code: {response.StatusCode}", modelErrorMessage: "Unable to delete patient", response: response);
            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
    }
}