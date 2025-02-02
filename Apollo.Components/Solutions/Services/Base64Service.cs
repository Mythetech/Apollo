using System.Text;
using System.Text.Json;
using Apollo.Components.Solutions;

namespace Apollo.Components.Solutions.Services;

public class Base64Service
{
    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new ISolutionItemConverter() }
    };

    public string EncodeSolution(SolutionModel solution)
    {
        var json = JsonSerializer.Serialize(solution, _options);
        var bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToBase64String(bytes);
    }
    
    public SolutionModel? DecodeSolution(string base64)
    {
        try
        {
            var bytes = Convert.FromBase64String(base64);
            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<SolutionModel>(json, _options);
        }
        catch
        {
            return null;
        }
    }
}