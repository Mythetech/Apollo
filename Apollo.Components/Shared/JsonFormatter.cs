using System.Text.Json;

namespace Apollo.Components.Shared;

public static class JsonFormatter
{
    private static readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
    };
    
    public static string Format(string code)
    {
        var serialized = JsonSerializer.Serialize(code);

        return JsonSerializer.Deserialize<string>(serialized, _options);
    }
    
}