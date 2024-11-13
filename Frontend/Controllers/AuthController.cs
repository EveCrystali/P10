using System.Security.Claims;
using Frontend.Models;
using Frontend.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
namespace Frontend.Controllers;

[Route("Auth")]
public class AuthController : Controller
{
    private readonly string _authServiceUrl;
    private readonly HttpClient _httpClient;
    private readonly HttpClientService _httpClientService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JwtValidationService _jwtValidationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(ILogger<AuthController> logger, IConfiguration configuration, HttpClientService httpClientService, IHttpContextAccessor httpContextAccessor, HttpClient httpClient, JwtValidationService jwtValidationService)
    {
        _logger = logger;
        _httpClientService = httpClientService;
        _httpContextAccessor = httpContextAccessor;
        _httpClient = httpClient;
        _authServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("Auth");
        _jwtValidationService = jwtValidationService;
    }

    [HttpGet("login")]
    public IActionResult Login() => View();

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginModel loginModel)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogError("Model state is not valid.");
            return View(loginModel);
        }

        HttpResponseMessage response = await _httpClient.PostAsJsonAsync($"{_authServiceUrl}/login", loginModel);

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

            // FUTURE: Redirect to previous page (if Login was called from a method needing a authentification) or home

            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
        _logger.LogError("Response is not Success Status Code.");
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(loginModel);
    }

    [HttpGet("register")]
    public IActionResult Register() => View();

    // [HttpPost("register")]
    // public async Task<IActionResult> Register(RegisterModel registerModel)
    // {
    //     if (!ModelState.IsValid)
    //     {
    //         return View(registerModel);
    //     }

    // using HttpRequestMessage request = new(HttpMethod.Post, $"{_authServiceUrl}/register");
    // request.Content = new StringContent(JsonConvert.SerializeObject(registerModel), Encoding.UTF8, "application/json");

    // using HttpResponseMessage response = await _httpClientServiceync(request);
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

        HttpRequestMessage request = new(HttpMethod.Post, $"{_authServiceUrl}/logout");
        HttpResponseMessage response = await _httpClientService.SendAsync(request);

        if (response.IsSuccessStatusCode)
        {
            HttpContext.Response.Cookies.Delete("AuthTokens");
            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }

        return BadRequest("Erreur lors de la déconnexion.");
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status()
    {
        string? tokenSerialized = _httpContextAccessor.HttpContext?.Request.Cookies["AuthTokens"];

        if (tokenSerialized != null)
        {
            AuthToken? authToken = JsonConvert.DeserializeObject<AuthToken>(tokenSerialized);
            if (authToken != null && !string.IsNullOrEmpty(authToken.Token))
            {
                ClaimsPrincipal? principal = _jwtValidationService.ValidateToken(authToken.Token);
                if (principal != null)
                {
                    string? username = principal.Identity?.Name ?? principal.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value;
                    return Ok(new
                    {
                        isAuthenticated = true,
                        username
                    });
                }
            }
        }
        return Ok(new
        {
            isAuthenticated = false,
            username = (string?)null
        });
    }
}