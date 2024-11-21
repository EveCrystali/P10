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
    private readonly IConfiguration _configuration;
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
        _configuration = configuration;
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
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.Now.AddDays(_configuration.GetSection("JwtSettings").GetValue<int>("RefreshTokenLifetimeDays"))
            };

            HttpContext.Response.Cookies.Append("AuthTokens", JsonConvert.SerializeObject(authToken), cookieOptions);

            // FUTURE: Redirect to previous page (if Login was called from a method needing a authentification) or home

            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }
        _logger.LogError("Response is not Success Status Code.");
        ModelState.AddModelError(string.Empty, "Invalid login attempt.");
        return View(loginModel);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!ModelState.IsValid)
        {
            _logger.LogError("Model state is not valid.");
            return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));
        }

        HttpRequestMessage request = new(HttpMethod.Post, $"{_authServiceUrl}/logout");
        HttpResponseMessage response = await _httpClientService.SendAsync(request);

        if (!response.IsSuccessStatusCode) return BadRequest("Erreur lors de la déconnexion.");

        HttpContext.Response.Cookies.Delete("AuthTokens");
        return RedirectToAction(nameof(Index), nameof(HomeController).Replace("Controller", ""));

    }

    [HttpGet("status")]
    public Task<IActionResult> Status()
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
                    return Task.FromResult<IActionResult>(Ok(new
                    {
                        isAuthenticated = true,
                        username
                    }));
                }
            }
        }
        return Task.FromResult<IActionResult>(Ok(new
        {
            isAuthenticated = false,
            username = (string?)null
        }));
    }
}