using System.Net;
using Frontend.Models;
using Frontend.Services;
using Microsoft.AspNetCore.Mvc;
namespace Frontend.Controllers;

[Route("patient")]
public class PatientsController : Controller
{
    private readonly DiabetesRiskPredictionService _diabetesRiskPredictionService;
    private readonly string _diabetesRiskPredictionServiceUrl;
    private readonly HttpClient _httpClient;

    private readonly HttpClientService _httpClientService;
    private readonly ILogger<PatientsController> _logger;
    private readonly string _noteServiceUrl;
    private readonly PatientService _patientService;
    private readonly string _patientServiceUrl;

    public PatientsController(ILogger<PatientsController> logger, HttpClient httpClient,
                              HttpClientService httpClientService,
                              IConfiguration configuration, PatientService patientService, DiabetesRiskPredictionService diabetesRiskPredictionService)
    {
        _logger = logger;
        _httpClient = httpClient;

        _httpClientService = httpClientService;
        _patientService = patientService;
        _patientServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("Patient");
        _noteServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("Note");
        _diabetesRiskPredictionServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("DiabetesRiskPrediction");
        _diabetesRiskPredictionService = diabetesRiskPredictionService;
    }

    public async Task<IActionResult> Index()
    {
        HttpResponseMessage response = await _httpClient.GetAsync(_patientServiceUrl);
        if (response.IsSuccessStatusCode)
        {
            List<Patient>? patients =
                await response.Content.ReadFromJsonAsync<List<Patient>>();
            if (patients != null)
            {
                foreach (Patient patient in patients)
                {
                    Console.WriteLine($"Patients: {patient.Id} {patient.FirstName} {patient.LastName}");
                }
            }

            return View(patients);
        }

        ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                               "Failed to load patients", "Unable to load patient");

        return View(new List<Patient>());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get Patient from BackendPatient
        HttpRequestMessage request1 = new(HttpMethod.Get, $"{_patientServiceUrl}/{id}");
        HttpResponseMessage responseFromPatientService = await _httpClientService.SendAsync(request1);

        int patientId = id;

        // Get Notes from BackendNote for the patient Id
        HttpRequestMessage request2 = new(HttpMethod.Get, $"{_noteServiceUrl}/patient/{patientId}");
        HttpResponseMessage responseFromNoteService = await _httpClientService.SendAsync(request2);

        if (responseFromPatientService.StatusCode == HttpStatusCode.Unauthorized ||
            responseFromNoteService.StatusCode == HttpStatusCode.Unauthorized)
        {
            return RedirectToAction(nameof(AuthController.Login), nameof(AuthController).Replace("Controller", ""));
        }

        if (responseFromPatientService.IsSuccessStatusCode && responseFromNoteService.IsSuccessStatusCode)
        {
            Patient? patient = await responseFromPatientService.Content.ReadFromJsonAsync<Patient>();
            List<Note>? notes = await responseFromNoteService.Content.ReadFromJsonAsync<List<Note>>();

            if (patient != null && notes != null)
            {
                // Time To to Get Diabetes Risk Prediction from BackendDiabetesRiskPrediction

                // First let's construct our needs
                PatientViewModel patientViewModel = PatientService.MapPatientNoteToPatientNotesViewModel(patient, notes);
                DiabetesRiskRequestModel diabetesRiskRequestModel = _diabetesRiskPredictionService.MapPatientViewModelAndNoteToDiabetesRiskRequestModel(patientViewModel);

                // Secondly let's ask DiabetesRiskPredictionService
                HttpRequestMessage request3 = new(HttpMethod.Get, $"{_diabetesRiskPredictionServiceUrl}/")
                {
                    Content = JsonContent.Create(diabetesRiskRequestModel)
                };

                // Finally let's manage the answer for DiabetesRiskPredictionService
                HttpResponseMessage responseFromDiabetesRiskService = await _httpClientService.SendAsync(request3);

                if (responseFromDiabetesRiskService.StatusCode == HttpStatusCode.Unauthorized)
                {
                    return RedirectToAction(nameof(AuthController.Login),
                                            nameof(AuthController).Replace("Controller", ""));
                }

                patientViewModel.DiabetesRiskPrediction = await responseFromDiabetesRiskService.Content.ReadFromJsonAsync<DiabetesRiskPrediction>();


                return View(patientViewModel);

            }
            return View();
        }

        ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, "Patient not found",
                                               "Unable to load patient details.");
        return View();
    }

    [HttpGet("create")]
    public IActionResult Create() => View();

    [HttpPost("create")]
    public async Task<IActionResult> Create(Patient patient)
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
            if (response.IsSuccessStatusCode)
            {
                Patient? createdPatient = await response.Content.ReadFromJsonAsync<Patient>();
                if (createdPatient != null)
                {
                    return RedirectToAction(nameof(Details), new { id = createdPatient.Id });
                }
                ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                                       "Failed to create patient", "Unable to create patient");
                return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
            }
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                                   $"Error from the server : {response.ReasonPhrase}",
                                                   "Unable to create patient", response);

            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
        ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                               "Model state is not valid.", "Unable to create patient");
        return View(patient);
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
            if (response.IsSuccessStatusCode)
            {
                Patient? patient = await response.Content.ReadFromJsonAsync<Patient>();
                return View(patient);
            }
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                                       "Patient not found.", "Unable to load patient for edit.");
                return View();
            }

            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                                   "Failed to load patient", "Unable to load patient for edit.");

            return View();
        }
        ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                               "Model state is not valid.", "Unable to load patient for edit.");
        return View();
    }

    [HttpPost("edit/{id}")]
    public async Task<IActionResult> Edit(Patient patient)
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
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Patient with id {PatientId} was successfully updated.", patient.Id);
                return RedirectToAction(nameof(Details), new { id = patient.Id });
            }
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                                   $"Failed to update patient with id {patient.Id}. Status code: {response.StatusCode}",
                                                   "Unable to update patient");
            return View(patient);
        }
        ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                               "Model state is not valid.", "Unable to update patient");
        return View(patient);
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
        if (response.IsSuccessStatusCode)
        {
            Patient? patient = await response.Content.ReadFromJsonAsync<Patient>();
            return View(patient);
        }
        ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                               $"Failed to load patient with id {id}. Status code: {response.StatusCode}",
                                               "Unable to load patient for deletion.", response);
        return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
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
        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
        ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                               $"Failed to delete patient with id {id}. Status code: {response.StatusCode}",
                                               "Unable to delete patient", response);
        return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
    }
}