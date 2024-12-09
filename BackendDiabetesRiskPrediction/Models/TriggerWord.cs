using System.ComponentModel.DataAnnotations;

namespace BackendDiabetesRiskPrediction.Models;

public class TriggerWordValidationAttribute(int minLength = 2, int maxLength = 50) : ValidationAttribute
{
    private readonly int _minLength = minLength;
    private readonly int _maxLength = maxLength;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not HashSet<string> words || words.Count == 0)
        {
            return new ValidationResult("La liste des mots déclencheurs ne peut pas être vide");
        }

        foreach (var word in words)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return new ValidationResult("Les mots déclencheurs ne peuvent pas être vides");
            }

            if (word.Length < _minLength || word.Length > _maxLength)
            {
                return new ValidationResult($"Les mots déclencheurs doivent avoir entre {_minLength} et {_maxLength} caractères");
            }
        }

        return ValidationResult.Success;
    }
}
