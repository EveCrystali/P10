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
    public async Task<ActionResult<DiabetesRisk>> GetDiabetesRisk([FromBody] PatientRiskRequest patientRiskRequest)
    {
        logger.LogDebug("GetDiabetesRisk called");
        logger.LogDebug($"Diabetes risk request : {patientRiskRequest}");
        DiabetesRiskPrediction diabetesRisk = await diabetesRiskNotePredictionService.DiabetesRiskPrediction(patientRiskRequest);
        logger.LogInformation($"Diabetes risk is : {diabetesRisk.DiabetesRisk}");
        return Ok(diabetesRisk);
    }
}