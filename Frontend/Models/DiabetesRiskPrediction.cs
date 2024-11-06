using System.ComponentModel.DataAnnotations;
namespace Frontend.Models;

public class DiabetesRiskPrediction
{
    [EnumDataType(typeof(DiabetesRisk))]
    public DiabetesRisk DiabetesRisk { get; set; } = DiabetesRisk.None;
}

public enum DiabetesRisk
{
    None,
    Borderline,
    InDanger,
    EarlyOnset
}