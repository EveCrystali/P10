using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackendPatient.Extensions;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    private const string Format = "yyyy-MM-dd";

    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (DateOnly.TryParseExact(reader.GetString(), Format, null, System.Globalization.DateTimeStyles.None, out var date))
        {
            return date;
        }
        throw new JsonException("Invalid date format. Expected format: yyyy-MM-dd");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString(Format));
    }

}
