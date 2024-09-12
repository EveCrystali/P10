using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace BackendPatient.Extensions;

public static class ValidationExtensions
{
    public static void Validate(this IValidatable entity)
    {
        List<ValidationResult> validationResults = [];
        ValidationContext validationContext = new(entity, null, null);
        bool isValid = Validator.TryValidateObject(entity, validationContext, validationResults, true);

        if (!isValid)
        {
            string errors = string.Join("; ", validationResults.Select(vr => vr.ErrorMessage));
            throw new ValidationException($"{entity.GetType().Name} is not valid: {errors}");
        }

        // Validate DateOnly properties
        entity.ValidateDateOnlyProperties();
    }

    public static void ValidateDateOnlyProperties(this IValidatable entity)
    {
        IEnumerable<System.Reflection.PropertyInfo> dateOnlyProperties = entity.GetType().GetProperties()
            .Where(prop => prop.PropertyType == typeof(DateOnly?) || prop.PropertyType == typeof(DateOnly));

        foreach (System.Reflection.PropertyInfo? prop in dateOnlyProperties)
        {
            DateOnly? value = prop.GetValue(entity) as DateOnly?;
            if (value != null && value.HasValue)
            {
                ValidateDateOnly(value, prop.Name);
            }
            else if (value == null && prop.PropertyType == typeof(DateOnly?))
            {
                return;
            }
            else if (value == null && prop.PropertyType == typeof(DateOnly))
            {
                throw new ValidationException($"{prop.Name} must not be null or empty");
            }
        }
    }

    public static void ValidateDateOnly(DateOnly? date, string propertyName)
    {
        if (date == null)
        {
            // if date is null, no validation is required
            return;
        }
        bool isValid = DateOnly.TryParseExact(date.Value.ToString("yyyy-MM-dd"),
                                           "yyyy-MM-dd",
                                           CultureInfo.InvariantCulture,
                                           DateTimeStyles.None,
                                           out DateOnly tempDate);
        if (!isValid)
        {
            throw new ValidationException($"{propertyName} must be in the format yyyy-MM-dd");
        }
    }
}