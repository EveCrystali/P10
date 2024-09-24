using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers;
public class PatientsController(HttpClient httpClient, ILogger<PatientsController> logger) : Controller
{
    private readonly HttpClient _httpClient = httpClient;

    private ILogger<PatientsController> _logger = logger;

    public async Task<IActionResult> Index()
    {
        HttpResponseMessage response = await _httpClient.GetAsync("https://localhost:5000/patient");
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

    public async Task<IActionResult> Details(int id)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"https://localhost:5000/patient/{id}");
        if (response.IsSuccessStatusCode)
        {
            Frontend.Models.Patient? patient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
            return View(patient);
        }
        ModelState.AddModelError(string.Empty, "Patient not found.");
        return View();
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Frontend.Models.Patient patient)
    {
        if (ModelState.IsValid)
        {
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("https://localhost:5000/patient", patient);
            if (response.IsSuccessStatusCode)
            {
                var createdPatient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
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
                _logger.LogError($"Error from the server : " + response.ReasonPhrase);
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

    public async Task<IActionResult> Edit(int id)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"https://localhost:5000/patient/{id}");
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

    [HttpPost]
    public async Task<IActionResult> Edit(Frontend.Models.Patient patient)
    {
        if (ModelState.IsValid)
        {
            _logger.LogInformation($"Updating patient with id {patient.Id} to {patient}");

            HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"https://localhost:5000/patient/{patient.Id}", patient);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation($"Patient with id {patient.Id} was successfully updated.");
                return RedirectToAction(nameof(Details), new { id = patient.Id });
            }
            else
            {
                _logger.LogError($"Failed to update patient with id {patient.Id}. Status code: {response.StatusCode}");
            }
        }
        else
        {
            _logger.LogError("Model state is not valid.");
        }

        ModelState.AddModelError(string.Empty, "Unable to update patient.");
        return View(patient);
    }

    public async Task<IActionResult> Delete(int id)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"https://localhost:5000/patient/{id}");
        if (response.IsSuccessStatusCode)
        {
            Frontend.Models.Patient? patient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
            return View(patient);
        }

        ModelState.AddModelError(response.StatusCode.ToString(), "Unable to load patient for deletion.");
        return View();
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"https://localhost:5000/patient/{id}");
        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction("Index", "Home");
        }

        ModelState.AddModelError(string.Empty, "Unable to delete patient.");
        return View();
    }

    private async Task<Frontend.Models.Patient?> GetPatientById(int id)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"https://localhost:5000/patient/{id}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
        }
        return null;
    }
}

