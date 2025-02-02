using System.Text.Json;
using Apollo.Contracts.Solutions;

namespace Apollo.Hosting;

public record SolutionPayload
{
    public required string Action { get; init; }
    public required Solution Solution { get; init; }
    
    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
    
    public static SolutionPayload? FromJson(string json) => 
        JsonSerializer.Deserialize<SolutionPayload>(json, JsonOptions);
} 