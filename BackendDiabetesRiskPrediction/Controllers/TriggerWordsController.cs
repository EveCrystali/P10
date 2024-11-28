using BackendDiabetesRiskPrediction.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BackendDiabetesRiskPrediction.Controllers;

[ApiController]
[Route("triggerwords")]
[Authorize(Policy = "RequirePractitionerRoleOrHigher")]
public class TriggerWordsController : ControllerBase
{
    private readonly ITriggerWordsService _triggerWordsService;
    private readonly ILogger<TriggerWordsController> _logger;

    public TriggerWordsController(ITriggerWordsService triggerWordsService, ILogger<TriggerWordsController> logger)
    {
        _triggerWordsService = triggerWordsService;
        _logger = logger;
    }

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
    public ActionResult SaveTriggerWords([FromBody] HashSet<string> triggerWords)
    {
        try
        {
            if (triggerWords == null || !triggerWords.Any())
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
    public ActionResult ResetToDefault()
    {
        try
        {
            var defaultWords = _triggerWordsService.ResetToDefault();
            return Ok(defaultWords);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la réinitialisation des mots déclencheurs");
            return StatusCode(500, "Erreur lors de la réinitialisation des mots déclencheurs");
        }
    }
}
