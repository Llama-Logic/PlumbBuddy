namespace PlumbBuddy.Services.ScriptApi;

public class PermissiveGuidConverter :
    JsonConverter<Guid>
{
    public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        Guid.Parse(reader.GetString()!);

    [SuppressMessage("Globalization", "CA1308: Normalize strings to uppercase", Justification = "Noneyo")]
    public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.ToString("n").ToLowerInvariant());
    }
}
