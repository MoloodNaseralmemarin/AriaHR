using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AriaHR.Shared.Converters;

public class UtcDateTimeJsonConverter : JsonConverter<DateTime>
{
    private const string Iso8601UtcFormat = "yyyy-MM-ddTHH:mm:ss.FFFFFFF'Z'";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && reader.TryGetDateTime(out var dateTime))
        {
            return dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            };
        }

        throw new JsonException($"Unable to convert '{reader.GetString()}' to DateTime.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utcDateTime = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        writer.WriteStringValue(utcDateTime.ToString(Iso8601UtcFormat, CultureInfo.InvariantCulture));
    }
}
