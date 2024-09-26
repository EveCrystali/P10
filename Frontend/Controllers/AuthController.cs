using Frontend.Controllers.Service;
using Frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Frontend.Controllers;

public class AuthController : Controller
{
    private readonly ILogger<AuthController> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _authServiceUrl; 
    private const string SetCookieHeader = "Set-Cookie";

    public AuthController(ILogger<AuthController> logger, HttpClient httpClient, IConfiguration configuration)
    {
        _logger = logger;
        _httpClient = httpClient;
        _authServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("Auth");
    }

    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginModel loginModel)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogError("Model state is not valid.");
            return View(loginModel);
        }

        HttpResponseMessage response = await _httpClient.PostAsync($"{_authServiceUrl}/login", new StringContent(JsonConvert.SerializeObject(loginModel), System.Text.Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Response is Success Status Code.");

            if (response.Headers.TryGetValues(SetCookieHeader, out IEnumerable<string>? setCookies))
            {
                foreach (string cookie in setCookies)
                {
                    Response.Headers.Append(SetCookieHeader, cookie);
                }
            }
            return RedirectToAction("Index", "Home");
        }
        else
        {
            _logger.LogError("Response is not Success Status Code.");
            ModelState.AddModelError(string.Empty, "Tentative de connexion invalide.");
            return View(loginModel);
        }
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterModel registerModel)
    {
        if (!ModelState.IsValid)
        {
            return View(registerModel);
        }


        HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"{_authServiceUrl}/register/", registerModel);

        if (response.IsSuccessStatusCode && response.Headers.TryGetValues(SetCookieHeader, out IEnumerable<string>? setCookies))
        {
            foreach (string cookie in setCookies)
            {
                Response.Headers.Append(SetCookieHeader, cookie);
            }

            return RedirectToAction("Index", "Home");
        }
        else
        {
            Dictionary<string, string[]>? errors = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();
            if (errors != null)
            {
                foreach (string desc in errors.SelectMany(error => error.Value))
                {
                    ModelState.AddModelError(string.Empty, desc);
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Une erreur est survenue.");
            }
            return View(registerModel);
        }
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        HttpResponseMessage response = await _httpClient.PostAsync($"{_authServiceUrl}/logout/", null);

        if (response.IsSuccessStatusCode)
        {
            Response.Cookies.Delete("P10AuthCookie");
            return RedirectToAction("Index", "Home");
        }

        return BadRequest("Erreur lors de la déconnexion.");
    }

}
