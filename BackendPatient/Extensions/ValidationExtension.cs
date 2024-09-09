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

        // Validate DateTime properties
        entity.ValidateDateTimeProperties();
    }

    public static void ValidateDateTimeProperties(this IValidatable entity)
    {
        IEnumerable<System.Reflection.PropertyInfo> dateTimeProperties = entity.GetType().GetProperties()
            .Where(prop => prop.PropertyType == typeof(DateTime?) || prop.PropertyType == typeof(DateTime));

        foreach (System.Reflection.PropertyInfo? prop in dateTimeProperties)
        {
            DateTime? value = prop.GetValue(entity) as DateTime?;
            if (value != null && value.HasValue)
            {
                ValidateDateTime(value, prop.Name);
            }
            else if (value == null && prop.PropertyType == typeof(DateTime?))
            {
                return;
            }
            else if (value == null && prop.PropertyType == typeof(DateTime))
            {
                throw new ValidationException($"{prop.Name} must not be null or empty");
            }
        }
    }

    public static void ValidateDateTime(DateTime? date, string propertyName)
    {
        if (date == null)
        {
            // if date is null, no validation is required
            return;
        }
        bool isValid = DateTime.TryParseExact(date.Value.ToString("yyyy-MM-dd"),
                                              "yyyy-MM-ddTHH:mm:ss.fffZ",
                                              CultureInfo.InvariantCulture,
                                              DateTimeStyles.AssumeUniversal,
                                              out DateTime tempDate);
        if (!isValid)
        {
            throw new ValidationException($"{propertyName} must be in the format yyyy-MM-dd");
        }
    }
}