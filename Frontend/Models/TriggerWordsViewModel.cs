namespace Frontend.Models;

using System.ComponentModel.DataAnnotations;

public class StringLengthHashSetAttribute(int minLength, int maxLength) : ValidationAttribute
{
    private readonly int _minLength = minLength;
    private readonly int _maxLength = maxLength;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is HashSet<string> hashSet)
        {
            foreach (var str in hashSet)
            {
                if (str.Length < _minLength || str.Length > _maxLength)
                {
                    return new ValidationResult($"Les mots déclencheurs doivent avoir entre {_minLength} et {_maxLength} caractères.");
                }
            }
        }
        return ValidationResult.Success;
    }
}

public class TriggerWordsViewModel
{
    [StringLengthHashSet(2, 50, ErrorMessage = "Les mots déclencheurs doivent avoir entre 2 et 50 caractères.")]
    public HashSet<string> TriggerWords { get; set; } = new HashSet<string>();
}
