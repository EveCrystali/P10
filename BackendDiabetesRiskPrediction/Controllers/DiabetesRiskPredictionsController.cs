using BackendDiabetesRiskPrediction.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BackendDiabetesRiskPrediction.Services;

namespace BackendDiabetesRiskPrediction.Controllers
{

    public class BackendDiabetesRiskPredictionController : ControllerBase
    {

    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "RequirePractitionerRoleOrHigher")]
    public class DiabetesRiskPrediction : ControllerBase
    {
        [HttpGet]
        public DiabetesRisk GetDiabetesRisk(List<Note> notes)
        {
            return DiabetesRiskPredictionNotesAnalysis(notes);
        }
    }
}
