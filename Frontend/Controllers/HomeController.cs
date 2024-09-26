using System.Diagnostics;
using Frontend.Controllers.Service;
using Frontend.Models;
using Microsoft.AspNetCore.Mvc;

namespace Frontend.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _homeServiceUrl; 

    public HomeController(ILogger<HomeController> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _homeServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("Home");
    }

    public async Task<IActionResult> Index()
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"{_homeServiceUrl}patient/");
        if (response.IsSuccessStatusCode)
        {
            List<Frontend.Models.Patient>? patients = await response.Content.ReadFromJsonAsync<List<Frontend.Models.Patient>>();
            return View(patients);
        }

        _logger.LogError("Failed to load patients from backend. Status Code: {0}", response.StatusCode);
        return View(new List<Frontend.Models.Patient>());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}