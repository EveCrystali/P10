using Frontend.Controllers.Service;
using Frontend.Models;
using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers;

[Route("patient")]
public class PatientsController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HttpClientService _httpClientService;
    private readonly ILogger<PatientsController> _logger;
    private readonly string _patientServiceUrl;
    private readonly PatientService _patientService;
    private readonly string _noteServiceUrl;

    public PatientsController(ILogger<PatientsController> logger, HttpClient httpClient, 
    IHttpContextAccessor httpContextAccessor, HttpClientService httpClientService, 
    IConfiguration configuration, PatientService patientService)
    {
        _logger = logger;
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
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

        ModelState.AddModelError(string.Empty, "Unable to load patients.");
        // FUTURE: Add TempData on the view
        TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
        return View(new List<Frontend.Models.Patient>());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // NOTE : responseFromPatientService status code 200 OK
        HttpResponseMessage responseFromPatientService = await _httpClient.GetAsync($"{_patientServiceUrl}/{id}");

        int patientId = id;

        // DONE: BUG: reponseFromNoteService is status code 401 because unauthorized
        // FUTURE: try to use the below line of code instead of the two above when Authorization is well implemented in the backend Note service
        // HttpResponseMessage responseFromNoteService = await _httpClient.GetAsync($"{_noteServiceUrl}/patient/{patientId}");

        HttpRequestMessage request = new(HttpMethod.Get, $"{_noteServiceUrl}/patient/{patientId}");
        HttpResponseMessage responseFromNoteService = await _httpClientService.SendAsync(request);

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

        // TODO : handle error from unauthorized if the user is not connected (401 Unauthorized) : should be redirected to login page
        ModelState.AddModelError(string.Empty, "Patient not found.");
        // FUTURE: Add TempData on the view
        TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
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

            if (response.IsSuccessStatusCode)
            {
                Models.Patient? createdPatient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
                if (createdPatient != null)
                {
                    return RedirectToAction(nameof(Details), new { id = createdPatient.Id });
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Failed to create patient.");
                    _logger.LogError("Failed to create patient.");
                    // FUTURE: Add TempData on the view
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
            ModelState.AddModelError(string.Empty, "Unable to create patient.");
            // FUTURE: Add TempData on the view
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
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

            if (response.IsSuccessStatusCode)
            {
                Frontend.Models.Patient? patient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
                return View(patient);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ModelState.AddModelError(string.Empty, "Patient not found.");
                _logger.LogError("Patient not found.");
                // FUTURE: Add TempData on the view
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                return View();
            }
            ModelState.AddModelError(string.Empty, "Unable to load patient for edit.");
            // FUTURE: Add TempData on the view
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            return View();
        }
        else
        {
            _logger.LogError("Model state is not valid.");
            ModelState.AddModelError(string.Empty, "Unable to load patient for edit.");
            // FUTURE: Add TempData on the view
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
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

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Patient with id {PatientId} was successfully updated.", patient.Id);
                return RedirectToAction(nameof(Details), new { id = patient.Id });
            }
            else
            {
                _logger.LogError("Failed to update patient with id {PatientId}. Status code: {StatusCode}", patient.Id, response.StatusCode);
                string errorContent = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(response.StatusCode.ToString(), "Unable to update patient.");
                // FUTURE: Add TempData on the view
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                return View(patient);
            }
        }
        else
        {
            _logger.LogError("Model state is not valid.");
            ModelState.AddModelError(string.Empty, "Unable to update patient.");
            // FUTURE: Add TempData on the view
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
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

        if (response.IsSuccessStatusCode)
        {
            Frontend.Models.Patient? patient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
            return View(patient);
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to load patient with id {PatientId}. Status code: {StatusCode}, Error: {Error}", id, response.StatusCode, errorContent);
            ModelState.AddModelError(response.StatusCode.ToString(), "Unable to load patient for deletion.");
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

        HttpRequestMessage request = new(HttpMethod.Delete, $"{_patientServiceUrl}/{id}");
        HttpResponseMessage response = await _httpClientService.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
        else
        {
            string errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Failed to delete patient with id {PatientId}. Status code: {StatusCode}, Error: {Error}", id, response.StatusCode, errorContent);
            ModelState.AddModelError(string.Empty, "Unable to delete patient.");
            TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
    }
}