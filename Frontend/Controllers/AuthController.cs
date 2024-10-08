using System.Text;
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

        HttpResponseMessage response = await _httpClient.PostAsync($"{_authServiceUrl}/login", 
                                                                    new StringContent(JsonConvert.SerializeObject(loginModel), 
                                                                    System.Text.Encoding.UTF8, "application/json"));

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Response is Success Status Code.");

            // Deserialize directly to AuthResponseModel
            AuthResponseModel? authResponseDeserialized = await response.Content.ReadFromJsonAsync<AuthResponseModel>();

            if (authResponseDeserialized == null || string.IsNullOrEmpty(authResponseDeserialized.Token) 
                    || string.IsNullOrEmpty(authResponseDeserialized.RefreshToken))
            {
                _logger.LogError("Token or refresh token is null or empty.");
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(loginModel);
            }

            // Create a combined object
            AuthToken authToken = new()
            {
                Token = authResponseDeserialized.Token,
                RefreshToken = authResponseDeserialized.RefreshToken
            };

            // Serialize to JSON and store in a cookie
            CookieOptions cookieOptions = new()
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            };

            HttpContext.Response.Cookies.Append("AuthTokens", JsonConvert.SerializeObject(authToken), cookieOptions);

            return RedirectToAction("Index", "Home");
        }
        else
        {
            _logger.LogError("Response is not Success Status Code.");
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(loginModel);
        }
    }

    [HttpGet("register")]
    public IActionResult Register()
    {
        return View();
    }

    // [HttpPost("register")]
    // public async Task<IActionResult> Register(RegisterModel registerModel)
    // {
    //     if (!ModelState.IsValid)
    //     {
    //         return View(registerModel);
    //     }

        // using HttpRequestMessage request = new(HttpMethod.Post, $"{_authServiceUrl}/register");
        // request.Content = new StringContent(JsonConvert.SerializeObject(registerModel), Encoding.UTF8, "application/json");

        // using HttpResponseMessage response = await _httpClient.SendAsync(request);
        // if (response.IsSuccessStatusCode)
        // {
        //     string? token = await response.Content.ReadFromJsonAsync<string>();
        //     if (string.IsNullOrEmpty(token))
        //     {
        //         ModelState.AddModelError(string.Empty, "Tentative de connexion invalide.");
        //         return View(registerModel);
        //     }

        //     // Store the JWT token (for example, in cookies or local storage)
        //     HttpContext.Response.Cookies.Append("JwtToken", token, new CookieOptions
        //     {
        //         HttpOnly = true,
        //         Secure = true,
        //         SameSite = SameSiteMode.Strict
        //     });

        //     return RedirectToAction("Index", "Home");
        // }
        // else
        // {
        //     Dictionary<string, string[]>? errors = await response.Content.ReadFromJsonAsync<Dictionary<string, string[]>>();
        //     if (errors != null)
        //     {
        //         foreach (string desc in errors.SelectMany(error => error.Value))
        //         {
        //             ModelState.AddModelError(string.Empty, desc);
        //         }
        //     }
        //     else
        //     {
        //         ModelState.AddModelError(string.Empty, "Une erreur est survenue.");
        //     }
        //     return View(registerModel);
        // }
    // }


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
            HttpContext.Response.Cookies.Delete("AuthTokens");
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
                string content = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Response content: {Content}", content);

                // Retourner directement le contenu sous forme d'objet JSON au lieu d'une chaîne de texte
                Dictionary<string, object>? jsonObject = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
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