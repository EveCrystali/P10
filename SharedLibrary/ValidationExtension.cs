using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;
namespace SharedLibrary;

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

    private static void ValidateDateOnlyProperties(this IValidatable entity)
    {
        IEnumerable<PropertyInfo> dateOnlyProperties = entity.GetType().GetProperties()
                                                             .Where(prop => prop.PropertyType == typeof(DateOnly?) || prop.PropertyType == typeof(DateOnly));

        foreach (PropertyInfo? prop in dateOnlyProperties)
        {
            DateOnly? value = prop.GetValue(entity) as DateOnly?;

            if (value != null)
            {
                ValidateDateOnly(value, prop.Name);
            }
            else
                switch (value)
                {
                    case null when prop.PropertyType == typeof(DateOnly?):
                        return;
                    case null when prop.PropertyType == typeof(DateOnly):
                        throw new ValidationException($"{prop.Name} must not be null or empty");
                }
        }
    }

    private static void ValidateDateOnly(DateOnly? date, string propertyName)
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
                                              out DateOnly _);
        if (!isValid)
        {
            throw new ValidationException($"{propertyName} must be in the format yyyy-MM-dd");
        }
    }
}