using Frontend.Controllers.Service;
using Frontend.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace Frontend.Controllers;

[Route("Auth")]
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

    [HttpGet("login")]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost("login")]
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

    [HttpGet("register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterModel registerModel)
    {
        if (!ModelState.IsValid)
        {
            return View(registerModel);
        }


        HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"{_authServiceUrl}/register", registerModel);

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

    // FIXME : Logout is not working properly

    // Note : BadREquest POST https://localhost:7000/Auth/Logout?returnUrl=%2F 400 (Bad Request)
    // Note : Uncaught (in promise) Error: QUOTA_BYTES_PER_ITEM quota exceeded
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {

        if (!ModelState.IsValid)
        {
            _logger.LogError("Model state is not valid.");
            return View();
        }

        HttpResponseMessage response = await _httpClient.PostAsync($"{_authServiceUrl}/logout", null);

        if (response.IsSuccessStatusCode)
        {
            Response.Cookies.Delete("P10AuthCookie");
            return RedirectToAction("Index", "Home");
        }

        return BadRequest("Erreur lors de la déconnexion.");
    }
    
    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        try
        {
            _logger.LogInformation("Checking status.");
            HttpResponseMessage response = await _httpClient.GetAsync($"{_authServiceUrl}/status");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Status request returned a success status code.");
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response content: {Content}", content);

                // Retourner directement le contenu sous forme d'objet JSON au lieu d'une chaîne de texte
                var jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                return Ok(jsonObject);
            }

            _logger.LogError("Status request returned a non-success status code.");
            return BadRequest("Erreur lors de la tentative de récupération du statut.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during Status check.");
            return StatusCode(500, "Erreur interne lors de la récupération du statut.");
        }
    }

}
