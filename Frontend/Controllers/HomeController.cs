using System.Diagnostics;
using Frontend.Models;
using Frontend.Services;
using Microsoft.AspNetCore.Mvc;
namespace Frontend.Controllers;

public class HomeController : Controller
{
    private readonly string _homeServiceUrl;
    private readonly HttpClient _httpClient;
    private readonly ILogger<HomeController> _logger;

    private readonly string _patientServiceUrl;

    public HomeController(ILogger<HomeController> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _homeServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("Home");
        _patientServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("Patient");
    }

    [Route("")]
    [Route("Home")]
    [Route("Home/Index")]
    [Route("Home/Index/{id?}")]
    public async Task<IActionResult> Index()
    {
        HttpResponseMessage response = await _httpClient.GetAsync($"{_patientServiceUrl}");
        if (response.IsSuccessStatusCode)
        {
            List<Patient>? patients = await response.Content.ReadFromJsonAsync<List<Patient>>();

            return View(patients);
        }

        _logger.LogError("Failed to load patients from backend. Status Code: {0}", response.StatusCode);
        return View(new List<Patient>());
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}