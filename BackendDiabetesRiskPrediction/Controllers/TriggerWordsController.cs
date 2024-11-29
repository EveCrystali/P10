using BackendDiabetesRiskPrediction.Services;
using BackendDiabetesRiskPrediction.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BackendDiabetesRiskPrediction.Controllers;

[ApiController]
[Route("triggerwords")]
[Authorize(Policy = "RequirePractitionerRoleOrHigher")]
public class TriggerWordsController(ITriggerWordsService triggerWordsService, ILogger<TriggerWordsController> logger) : ControllerBase
{
    private readonly ITriggerWordsService _triggerWordsService = triggerWordsService;
    private readonly ILogger<TriggerWordsController> _logger = logger;

    [HttpGet]
    public ActionResult<HashSet<string>> GetTriggerWords()
    {
        try
        {
            var words = _triggerWordsService.GetTriggerWords();
            return Ok(words);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la lecture des mots déclencheurs");
            return StatusCode(500, "Erreur lors de la lecture des mots déclencheurs");
        }
    }

    [HttpPost]
    public ActionResult SaveTriggerWords([FromBody, TriggerWordValidation] HashSet<string> triggerWords)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            if (triggerWords == null || triggerWords.Count == 0)
            {
                return BadRequest("La liste des mots déclencheurs ne peut pas être vide");
            }

            if (triggerWords.Any(word => string.IsNullOrWhiteSpace(word)))
            {
                return BadRequest("Les mots déclencheurs ne peuvent pas être vides");
            }

            _triggerWordsService.SaveTriggerWords(triggerWords);
            return Ok();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la sauvegarde des mots déclencheurs");
            return StatusCode(500, "Erreur lors de la sauvegarde des mots déclencheurs");
        }
    }

    [HttpPost("reset")]
    public ActionResult ResetTriggerWords()
    {
        logger.LogDebug("ResetToDefault called");
        try
        {
            var defaultWords = _triggerWordsService.ResetToDefault();
            logger.LogInformation("ResetToDefault completed");
            return Ok(defaultWords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la réinitialisation des mots déclencheurs");
            return StatusCode(500, "Erreur lors de la réinitialisation des mots déclencheurs");
        }
    }
}
