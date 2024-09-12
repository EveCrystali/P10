using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Frontend.Models;
using BackendPatient.Data;
using Microsoft.EntityFrameworkCore;


namespace Frontend.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HttpClient _httpClient;

    public HomeController(ILogger<HomeController> logger, HttpClient httpClient)
    {
        _logger = logger;
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

         _logger.LogError("Failed to load patients from backend. Status Code: {0}", response.StatusCode);
        return View(new List<Frontend.Models.Patient>());
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
