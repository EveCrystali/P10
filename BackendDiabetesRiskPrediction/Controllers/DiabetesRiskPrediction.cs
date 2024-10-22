using BackendDiabetesRiskPrediction.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BackendDiabetesRiskPrediction.Services;

namespace BackendDiabetesRiskPrediction.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Policy = "RequirePractitionerRoleOrHigher")]
public class BackendDiabetesRiskPredictionsController(DiabetesRiskNotePredictionService diabetesRiskNotePredictionService) : ControllerBase
{
    private readonly DiabetesRiskNotePredictionService _diabetesRiskNotePredictionService = diabetesRiskNotePredictionService;


    [HttpGet]
    public DiabetesRisk GetDiabetesRisk(List<Note> notes, BackendPatient.Models.Patient patient)
    {
        DiabetesRisk diabetesRisk = _diabetesRiskNotePredictionService.DiabetesRiskPredictionNotesAnalysis(notes);
        return diabetesRisk;
    }


}
