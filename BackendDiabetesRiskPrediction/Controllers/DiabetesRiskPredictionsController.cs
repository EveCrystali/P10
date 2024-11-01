using BackendDiabetesRiskPrediction.Models;
using BackendDiabetesRiskPrediction.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace BackendDiabetesRiskPrediction.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = "RequirePractitionerRoleOrHigher")]
public class BackendDiabetesRiskPredictionsController(DiabetesRiskNotePredictionService diabetesRiskNotePredictionService) : ControllerBase
{
    private readonly DiabetesRiskNotePredictionService _diabetesRiskNotePredictionService = diabetesRiskNotePredictionService;


    [HttpGet]
    public async Task<DiabetesRisk> GetDiabetesRisk([FromBody] DiabetesRiskRequest diabetesRiskRequest)
    {
        DiabetesRisk diabetesRisk = await _diabetesRiskNotePredictionService.DiabetesRiskPrediction(diabetesRiskRequest.NotesRiskInfo, diabetesRiskRequest.PatientRiskInfo);
        return diabetesRisk;
    }
}