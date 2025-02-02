using System.Text.Json;

namespace Apollo.Components.Shared;

public static class CodeFormatter
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
    };
    
    public static string Format(string code)
    {
        var serialized = JsonSerializer.Serialize(code);
        //JsonDocument document = JsonDocument.Parse(serialized);

        return JsonSerializer.Deserialize<string>(serialized, _options);
    }
    
}