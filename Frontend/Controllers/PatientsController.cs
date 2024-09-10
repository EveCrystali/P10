using Microsoft.AspNetCore.Mvc;
using Frontend.Models;

namespace Frontend.Controllers;
public class PatientsController : Microsoft.AspNetCore.Mvc.Controller
{
    private readonly HttpClient _httpClient;

    public PatientsController(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IActionResult> Index()
    {
        HttpResponseMessage response = await _httpClient.GetAsync("https://localhost:5000/api/patient");
        if (response.IsSuccessStatusCode)
        {
            List<Frontend.Models.Patient>? patients = await response.Content.ReadFromJsonAsync<List<Frontend.Models.Patient>>();
            return View(patients);
        }
        ModelState.AddModelError(string.Empty, "Unable to load patients.");
        return View(new List<Frontend.Models.Patient>());
    }

    public async Task<IActionResult> Details(int id)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"https://localhost:5000/api/patient/{id}");
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
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("https://localhost:5000/api/patient", patient);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Index));
            }
        }

        ModelState.AddModelError(string.Empty, "Unable to create patient.");
        return View(patient);
    }

    public async Task<IActionResult> Edit(int id)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"https://localhost:5000/api/patient/{id}");
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

    [HttpPut]
    public async Task<IActionResult> Edit(int id, Frontend.Models.Patient patient)
    {
        if (ModelState.IsValid)
        {
            HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"https://localhost:5000/api/patient/{id}", patient);
            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction(nameof(Details), new { id });
            }
        }

        ModelState.AddModelError(string.Empty, "Unable to update patient.");
        return View(patient);
    }

    public async Task<IActionResult> Delete(int id)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"https://localhost:5000/api/patient/{id}");
        if (response.IsSuccessStatusCode)
        {
            Frontend.Models.Patient? patient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
            return View(patient);
        }

        ModelState.AddModelError(string.Empty, "Unable to load patient for deletion.");
        return View();
    }

    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        HttpResponseMessage response = await _httpClient.DeleteAsync($"https://localhost:5000/api/patient/{id}");
        if (response.IsSuccessStatusCode)
        {
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, "Unable to delete patient.");
        return View();
    }

    private async Task<Frontend.Models.Patient?> GetPatientById(int id)
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"https://localhost:5000/api/patient/{id}");
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
        }
        return null;
    }
}
