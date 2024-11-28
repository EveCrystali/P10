using Frontend.Extensions;
using Frontend.Models;
using Frontend.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Frontend.Controllers;

[Route("TriggerWords")]
public class TriggerWordsController : Controller
{
    private readonly HttpClientService _httpClientService;
    private readonly ILogger<TriggerWordsController> _logger;

    private readonly string _diabetesRiskPredictionServiceUrl;

    private readonly string _controllerAuthName = nameof(AuthController).Replace("Controller", "");
    private readonly string _controllerHomeControllerName = nameof(HomeController).Replace("Controller", "");

    public TriggerWordsController(HttpClientService httpClientService, ILogger<TriggerWordsController> logger, IConfiguration configuration)
    {
        _logger = logger;
        _httpClientService = httpClientService;
        _diabetesRiskPredictionServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("DiabetesRiskPrediction");

    }


    [Route("")]
    public async Task<IActionResult> Index()
    {
        try
        {
            HttpRequestMessage requestForTriggerWords = new(HttpMethod.Get, $"{_diabetesRiskPredictionServiceUrl}/triggerwords");
            HttpResponseMessage responseForTriggerWords = await _httpClientService.SendAsync(requestForTriggerWords);

            IActionResult? authResult = this.HandleAuthorizationResponse(responseForTriggerWords.StatusCode, _logger);
            if (authResult != null) return authResult;

            if (responseForTriggerWords.IsSuccessStatusCode)
            {
                string content = await responseForTriggerWords.Content.ReadAsStringAsync();
                var triggerWords = JsonSerializer.Deserialize<HashSet<string>>(content);
                var viewModel = new TriggerWordsViewModel { TriggerWords = triggerWords ?? [] };
                return View(viewModel);
            }

            _logger.LogError("Erreur lors de la récupération des mots déclencheurs: {StatusCode}", responseForTriggerWords.StatusCode);
            return View(new TriggerWordsViewModel());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la récupération des mots déclencheurs");
            return View(new TriggerWordsViewModel());
        }
    }

    [HttpPost]
    public async Task<IActionResult> SaveTriggerWords([FromForm] string[] triggerWords)
    {
        try
        {
            HttpRequestMessage requestForTriggerWords = new(HttpMethod.Post, $"{_diabetesRiskPredictionServiceUrl}/triggerwords")
            {
                Content = new StringContent(JsonSerializer.Serialize(triggerWords), System.Text.Encoding.UTF8, "application/json")
            };
            HttpResponseMessage responseForTriggerWords = await _httpClientService.SendAsync(requestForTriggerWords);

            IActionResult? authResult = this.HandleAuthorizationResponse(responseForTriggerWords.StatusCode, _logger);
            if (authResult != null) return authResult;

            if (responseForTriggerWords.IsSuccessStatusCode)
            {
                TempData["Success"] = "Mots déclencheurs sauvegardés avec succès";
            }
            else
            {
                TempData["Error"] = "Erreur lors de la sauvegarde des mots déclencheurs";
                _logger.LogError("Erreur lors de la sauvegarde des mots déclencheurs: {StatusCode}", responseForTriggerWords.StatusCode);
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Erreur lors de la sauvegarde des mots déclencheurs";
            _logger.LogError(ex, "Erreur lors de la sauvegarde des mots déclencheurs");
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Route("reset")]
    public async Task<IActionResult> Reset()
    {
        try
        {
            HttpRequestMessage requestForTriggerWords = new(HttpMethod.Post, $"{_diabetesRiskPredictionServiceUrl}/triggerwords/reset");
            HttpResponseMessage responseForTriggerWords = await _httpClientService.SendAsync(requestForTriggerWords);

            IActionResult? authResult = this.HandleAuthorizationResponse(responseForTriggerWords.StatusCode, _logger);
            if (authResult != null) return authResult;

            if (responseForTriggerWords.IsSuccessStatusCode)
            {
                TempData["Success"] = "Mots déclencheurs réinitialisés avec succès";
            }
            else
            {
                TempData["Error"] = "Erreur lors de la réinitialisation des mots déclencheurs";
                _logger.LogError("Erreur lors de la réinitialisation des mots déclencheurs: {StatusCode}", responseForTriggerWords.StatusCode);
            }
        }
        catch (Exception ex)
        {
            TempData["Error"] = "Erreur lors de la réinitialisation des mots déclencheurs";
            _logger.LogError(ex, "Erreur lors de la réinitialisation des mots déclencheurs");
        }

        return RedirectToAction(nameof(Index));
    }
}
