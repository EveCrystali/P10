using Microsoft.AspNetCore.Mvc;
using BackendPatient.Models;

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
        HttpResponseMessage response = await _httpClient.GetAsync("http://localhost:5000/patients");
        var patients = await response.Content.ReadFromJsonAsync<List<Patient>>();
        return View(patients);
    }
}
