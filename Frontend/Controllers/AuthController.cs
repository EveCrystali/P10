using Microsoft.AspNetCore.Mvc;
using Frontend.Models;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Identity;

namespace Frontend.Controllers;

public class AuthController(ILogger<AuthController> logger, HttpClient httpClient) : Controller
{
    private readonly ILogger<AuthController> _logger = logger;

    private readonly HttpClient _httpClient = httpClient;


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
            return View(loginModel);
        }

        // FIXME: Is that correct ? 
        // Old way :
        // var response = await _httpClient.PostAsJsonAsync("/auth/login", loginModel);
        // New way :
        var response = await _httpClient.PostAsync("/auth/login", new StringContent(JsonConvert.SerializeObject(loginModel), System.Text.Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            // Récupérer les cookies de la réponse et les ajouter à la réponse du Frontend
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var cookie in setCookies)
                {
                    // Ajuster le cookie si nécessaire
                    Response.Headers.Append("Set-Cookie", cookie);
                }
            }
            return RedirectToAction("Index", "Home");
        }
        else
        {
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

        var response = await _httpClient.PostAsJsonAsync("api/auth/register", registerModel);

        if (response.IsSuccessStatusCode)
        {
            // Récupérer les cookies de la réponse et les ajouter à la réponse du Frontend
            if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
            {
                foreach (var cookie in setCookies)
                {
                    // Ajuster le cookie si nécessaire
                    Response.Headers.Append("Set-Cookie", cookie);
                }
            }
            return RedirectToAction("Index", "Home");
        }
        else
        {
            // Récupérer et afficher les erreurs
            var errors = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();
            if (errors != null)
            {
                foreach (var error in errors)
                {
                    foreach (var desc in error.Value)
                    {
                        ModelState.AddModelError(string.Empty, desc);
                    }
                }
                return View(registerModel);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Une erreur est survenue.");
                return View(registerModel);
            }
        }
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        var response = await _httpClient.PostAsync("api/auth/logout", null);

        if (response.IsSuccessStatusCode)
        {
            // Supprimer le cookie d'authentification
            Response.Cookies.Delete("P10AuthCookie");
            return RedirectToAction("Index", "Home");
        }

        return BadRequest("Erreur lors de la déconnexion.");
    }
}
