using Frontend.Controllers.Service;
using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers;

[Route("patient")]
public class PatientsController : Controller
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PatientsController> _logger;
    private readonly string _patientServiceUrl;

    public PatientsController(ILogger<PatientsController> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _patientServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("Patient");
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
        return View(new List<Frontend.Models.Patient>());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Details(int id)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"{_patientServiceUrl}/{id}");
        if (response.IsSuccessStatusCode)
        {
            Frontend.Models.Patient? patient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
            return View(patient);
        }
        ModelState.AddModelError(string.Empty, "Patient not found.");
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
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync(_patientServiceUrl, patient);
            if (response.IsSuccessStatusCode)
            {
                Models.Patient? createdPatient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
                if (createdPatient != null)
                {
                    return RedirectToAction(nameof(Details), new { id = createdPatient.Id });
                }
                else
                {
                    return RedirectToAction(nameof(Index), nameof(HomeController));
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Error from the server");
                _logger.LogError("Error from the server : {ReasonPhrase}", response.ReasonPhrase);
                return RedirectToAction(nameof(Index), nameof(HomeController));
            }
        }
        else
        {
            _logger.LogError("Model state is not valid.");
            ModelState.AddModelError(string.Empty, "Unable to create patient.");
            return View(patient);
        }
    }

    [HttpGet("edit/{id}")]
    public async Task<IActionResult> Edit(int id)
    {
        if (ModelState.IsValid)
        {
            HttpResponseMessage response = await _httpClient.GetAsync($"{_patientServiceUrl}/{id}");
            if (response.IsSuccessStatusCode)
            {
                Frontend.Models.Patient? patient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
                return View(patient);
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                ModelState.AddModelError(string.Empty, "Patient not found.");
                return View();
            }
            ModelState.AddModelError(string.Empty, "Unable to load patient for edit.");
            return View();
        }
        else
        {
            _logger.LogError("Model state is not valid.");
            ModelState.AddModelError(string.Empty, "Unable to load patient for edit.");
            return View();
        }
    }

    [HttpPost("edit/{id}")]
    public async Task<IActionResult> Edit(Frontend.Models.Patient patient)
    {
        if (ModelState.IsValid)
        {
            _logger.LogInformation("Updating patient with id {PatientId} to {Patient}", patient.Id, patient);
            HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"{_patientServiceUrl}/{patient.Id}", patient);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Patient with id {PatientId} was successfully updated.", patient.Id);
                return RedirectToAction(nameof(Details), new { id = patient.Id });
            }
            else
            {
                _logger.LogError("Failed to update patient with id {PatientId}. Status code: {StatusCode}", patient.Id, response.StatusCode);
            }
        }
        else
        {
            _logger.LogError("Model state is not valid.");
        }

        ModelState.AddModelError(string.Empty, "Unable to update patient.");
        return View(patient);
    }

    [HttpGet("delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {

        HttpResponseMessage response = await _httpClient.GetAsync($"{_patientServiceUrl}/{id}");
        if (response.IsSuccessStatusCode)
        {
            Frontend.Models.Patient? patient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
            return View(patient);
        }

        ModelState.AddModelError(response.StatusCode.ToString(), "Unable to load patient for deletion.");
        return View();
    }

    [HttpPost("delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"{_patientServiceUrl}/{id}");
        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "Unable to delete patient.");
        return View();
    }

}