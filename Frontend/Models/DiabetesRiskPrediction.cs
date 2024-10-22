using System.ComponentModel.DataAnnotations;

namespace Frontend.Models;

public class DiabetesRiskPrediction
{
    [EnumDataType(typeof(DiabetesRisk))]
    public DiabetesRisk DiabetesRisk { get; set; }
}

public enum DiabetesRisk
{
    None,
    Borderline,
    InDanger,
    EarlyOnset
}