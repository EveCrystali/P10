using System;
using System.ComponentModel.DataAnnotations;

namespace BackendPatient.Models;

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
