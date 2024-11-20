using BackendDiabetesRiskPrediction.Models;
using BackendDiabetesRiskPrediction.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BackendDiabetesRiskPrediction.Controllers;

[Route("diabetesriskprediction")]
[ApiController]
[Authorize(Policy = "RequirePractitionerRoleOrHigher")]
public class BackendDiabetesRiskPredictionsController(DiabetesRiskNotePredictionService diabetesRiskNotePredictionService, ILogger<BackendDiabetesRiskPredictionsController> logger) : ControllerBase
{


    [HttpGet]
    public async Task<ActionResult<DiabetesRisk>> GetDiabetesRisk([FromBody] DiabetesRiskRequest diabetesRiskRequest)
    {
        logger.LogDebug("GetDiabetesRisk called");
        logger.LogDebug($"Diabetes risk request : {diabetesRiskRequest}");
        DiabetesRiskPrediction diabetesRisk = await diabetesRiskNotePredictionService.DiabetesRiskPrediction(diabetesRiskRequest.NotesRiskInfo, diabetesRiskRequest.PatientRiskInfo);
        logger.LogInformation($"Diabetes risk is : {diabetesRisk}");
        return Ok(diabetesRisk);
    }
}