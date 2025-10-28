using System.Text.Json;
using System.Text.Json.Serialization;

namespace VHS_frontend.Helpers
{
    public class TimeOnlyJsonConverter : JsonConverter<TimeOnly>
    {
        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (value == null)
            {
                throw new JsonException("TimeOnly value cannot be null");
            }

            // Try to parse the time value
            if (TimeOnly.TryParse(value, out var result))
            {
                return result;
            }

            // If parsing fails, try to parse as TimeSpan
            if (TimeSpan.TryParse(value, out var timeSpan))
            {
                return TimeOnly.FromTimeSpan(timeSpan);
            }

            throw new JsonException($"Unable to convert \"{value}\" to {nameof(TimeOnly)}");
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("HH:mm"));
        }
    }

    public class DateOnlyJsonConverter : JsonConverter<DateOnly>
    {
        public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (value == null)
            {
                throw new JsonException("DateOnly value cannot be null");
            }

            if (DateOnly.TryParse(value, out var result))
            {
                return result;
            }

            throw new JsonException($"Unable to convert \"{value}\" to {nameof(DateOnly)}");
        }

        public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
        }
    }
}



