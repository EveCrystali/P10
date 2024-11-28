using System.Net;
using Frontend.Extensions;
using Frontend.Models;
using Frontend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Frontend.Controllers;

[Route("patient")]
public class PatientsController : Controller
{
    private readonly string _controllerAuthName = nameof(AuthController).Replace("Controller", "");
    private readonly string _controllerHomeControllerName = nameof(HomeController).Replace("Controller", "");
    private readonly string _diabetesRiskPredictionServiceUrl;
    private readonly HttpClientService _httpClientService;
    private readonly ILogger<PatientsController> _logger;
    private readonly string _noteServiceUrl;
    private readonly string _patientServiceUrl;

    public PatientsController(ILogger<PatientsController> logger,
                              HttpClientService httpClientService,
                              IConfiguration configuration)
    {
        _logger = logger;
        _httpClientService = httpClientService;
        _patientServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("Patient");
        _noteServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("Note");
        _diabetesRiskPredictionServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("DiabetesRiskPrediction");
    }


    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get Patient from BackendPatient
        HttpRequestMessage requestForBackendPatient = new(HttpMethod.Get, $"{_patientServiceUrl}/{id}");
        HttpResponseMessage responseFromPatientService = await _httpClientService.SendAsync(requestForBackendPatient);

        // Get Notes from BackendNote for the patient Id
        HttpRequestMessage requestForBackendNote = new(HttpMethod.Get, $"{_noteServiceUrl}/patient/{id}");
        HttpResponseMessage responseFromNoteService = await _httpClientService.SendAsync(requestForBackendNote);

        IActionResult? authResult = this.HandleAuthorizationResponse(responseFromNoteService.StatusCode, _logger);
        if (authResult != null) return authResult;

        if (responseFromPatientService.IsSuccessStatusCode && responseFromNoteService.IsSuccessStatusCode)
        {
            Patient? patient = await responseFromPatientService.Content.ReadFromJsonAsync<Patient>();
            List<Note>? notes = await responseFromNoteService.Content.ReadFromJsonAsync<List<Note>>();

            if (patient == null)
            {
                _logger.LogError("Patient not found");
                return NotFound("Patient non trouvé");
            }

            notes ??= [];

            // Time To Get Diabetes Risk Prediction from BackendDiabetesRiskPrediction

            // First let's construct our needs
            PatientViewModel patientViewModel = PatientService.MapPatientNoteToPatientNotesViewModel(patient, notes);
            PatientRiskRequest patientRiskRequest = DiabetesRiskPredictionService.MapPatientViewModelToPatientRiskInfo(patientViewModel);

            // Secondly let's ask DiabetesRiskPredictionService
            HttpRequestMessage requestForDiabetesRiskPredictionService = new(HttpMethod.Get, $"{_diabetesRiskPredictionServiceUrl}/")
            {
                Content = JsonContent.Create(patientRiskRequest)
            };
            _logger.LogInformation("Requesting Diabetes Risk Prediction for patient with id {PatientId}, Date of birth: {DateOfBirth}, Gender: {Gender}",
                                                patientRiskRequest.Id.ToString(), patientRiskRequest.DateOfBirth.ToString(), patientRiskRequest.Gender);

            // Finally let's manage the answer for DiabetesRiskPredictionService
            HttpResponseMessage responseFromDiabetesRiskService = await _httpClientService.SendAsync(requestForDiabetesRiskPredictionService);

            IActionResult? authResult2 = this.HandleAuthorizationResponse(responseFromDiabetesRiskService.StatusCode, _logger);
            if (authResult2 != null) return authResult2;

            _logger.LogDebug("Reading content from Json now...");

            patientViewModel.DiabetesRiskPrediction = await responseFromDiabetesRiskService.Content.ReadFromJsonAsync<DiabetesRiskPrediction>();

            return View(patientViewModel);
        }

        ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData, "Patient not found",
                                               "Unable to load patient details.");
        return View();
    }

    [HttpGet("create")]
    [Authorize(Policy = "RequirePractitionerRoleOrHigher")]
    public async Task<IActionResult> Create() => View();


    [HttpPost("create")]
    [Authorize(Policy = "RequirePractitionerRoleOrHigher")]
    public async Task<IActionResult> Create(Patient patient)
    {
        if (ModelState.IsValid)
        {
            HttpRequestMessage request = new(HttpMethod.Post, $"{_patientServiceUrl}/")
            {
                Content = JsonContent.Create(patient)
            };
            HttpResponseMessage response = await _httpClientService.SendAsync(request);

            IActionResult? authResult = this.HandleAuthorizationResponse(response.StatusCode, _logger);
            if (authResult != null) return authResult;

            if (response.IsSuccessStatusCode)
            {
                Patient? createdPatient = await response.Content.ReadFromJsonAsync<Patient>();
                if (createdPatient != null)
                {
                    return RedirectToAction(nameof(Details), new
                    {
                        id = createdPatient.Id
                    });
                }
                ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                                       "Failed to create patient", "Unable to create patient");
                return RedirectToAction(nameof(Index), _controllerHomeControllerName);
            }
            ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                                   $"Error from the server : {response.ReasonPhrase}",
                                                   "Unable to create patient", response);

            return RedirectToAction(nameof(Index), _controllerHomeControllerName);
        }
        ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                               "Model state is not valid.", "Unable to create patient");
        return View(patient);
    }

    [HttpGet("edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
    {
        if (ModelState.IsValid)
        {
            HttpRequestMessage request = new(HttpMethod.Get, $"{_patientServiceUrl}/{id}");
            HttpResponseMessage response = await _httpClientService.SendAsync(request);

            IActionResult? authResult = this.HandleAuthorizationResponse(response.StatusCode, _logger);
            if (authResult != null) return authResult;

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

    [HttpPost("edit/{id:int}")]
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

            IActionResult? authResult = this.HandleAuthorizationResponse(response.StatusCode, _logger);
            if (authResult != null) return authResult;

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Patient with id {PatientId} was successfully updated.", patient.Id);
                return RedirectToAction(nameof(Details), new
                {
                    id = patient.Id
                });
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

    [HttpGet("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        HttpRequestMessage request = new(HttpMethod.Get, $"{_patientServiceUrl}/{id}");
        HttpResponseMessage response = await _httpClientService.SendAsync(request);

        IActionResult? authResult = this.HandleAuthorizationResponse(response.StatusCode, _logger);
        if (authResult != null) return authResult;

        if (response.IsSuccessStatusCode)
        {
            Patient? patient = await response.Content.ReadFromJsonAsync<Patient>();
            return View(patient);
        }
        ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                               $"Failed to load patient with id {id}. Status code: {response.StatusCode}",
                                               "Unable to load patient for deletion.", response);
        return RedirectToAction(nameof(Index), _controllerHomeControllerName);
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        HttpRequestMessage request = new(HttpMethod.Delete, $"{_patientServiceUrl}/{id}");
        HttpResponseMessage response = await _httpClientService.SendAsync(request);

        IActionResult? authResult = this.HandleAuthorizationResponse(response.StatusCode, _logger);
        if (authResult != null) return authResult;

        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index), _controllerHomeControllerName);
        }
        ErrorHandlingUtils.HandleErrorResponse(_logger, ModelState, TempData,
                                               $"Failed to delete patient with id {id}. Status code: {response.StatusCode}",
                                               "Unable to delete patient", response);
        return RedirectToAction(nameof(Index), _controllerHomeControllerName);
    }
}