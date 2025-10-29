using System.Text.Json;
using System.Text.Json.Serialization;

namespace VHS_frontend.Areas.Provider.Models.Schedule
{
    public class TimeOnlyStringConverter : JsonConverter<TimeOnly>
    {
        public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetString();
            if (string.IsNullOrEmpty(value))
                return default;
            
            return TimeOnly.Parse(value);
        }

        public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString("HH:mm"));
        }
    }
}





