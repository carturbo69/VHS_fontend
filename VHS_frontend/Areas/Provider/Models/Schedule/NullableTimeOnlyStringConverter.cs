using System.Text.Json;
using System.Text.Json.Serialization;

namespace VHS_frontend.Areas.Provider.Models.Schedule
{
    public class NullableTimeOnlyStringConverter : JsonConverter<TimeOnly?>
    {
        public override TimeOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;
            
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return null;
            
            return TimeOnly.Parse(value);
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
                writer.WriteStringValue(value.Value.ToString("HH:mm"));
            else
                writer.WriteNullValue();
        }
    }
}








