using System.Text.Json;
using System.Text.Json.Serialization;

namespace Apollo.Components.Solutions;

public class ISolutionItemConverter : JsonConverter<ISolutionItem>
{
    public override ISolutionItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var jsonObject = doc.RootElement;

        // Check for the discriminator property
        if (jsonObject.TryGetProperty("Type", out var typeProperty))
        {
            var type = typeProperty.GetString();
            switch (type)
            {
                case nameof(SolutionFile):
                    return JsonSerializer.Deserialize<SolutionFile>(jsonObject.GetRawText(), options)!;
                case nameof(Folder):
                    return JsonSerializer.Deserialize<Folder>(jsonObject.GetRawText(), options)!;
                default:
                    throw new NotSupportedException($"Type '{type}' is not supported.");
            }
        }

        throw new JsonException("Missing type discriminator property.");
    }

    public override void Write(Utf8JsonWriter writer, ISolutionItem value, JsonSerializerOptions options)
    {
        var typeDiscriminator = value is SolutionFile ? nameof(SolutionFile) : nameof(Folder);

        writer.WriteStartObject();
        writer.WriteString("Type", typeDiscriminator);

        var json = JsonSerializer.Serialize(value, value.GetType(), options);
        using var doc = JsonDocument.Parse(json);

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            property.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}