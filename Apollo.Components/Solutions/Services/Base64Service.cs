using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Apollo.Components.Solutions;
using Microsoft.Extensions.Logging;

namespace Apollo.Components.Solutions.Services;

public class Base64Service
{
    private readonly ILogger<Base64Service> _logger;

    public Base64Service(ILogger<Base64Service> logger)
    {
        _logger = logger;
    }

    private readonly JsonSerializerOptions _options = new()
    {
        Converters = { new ISolutionItemConverter() }
    };

    public string EncodeSolution(SolutionModel solution)
    {
        var json = JsonSerializer.Serialize(solution, _options);
        using var memoryStream = new MemoryStream();
        using (var deflateStream = new DeflateStream(memoryStream, CompressionMode.Compress))
        {
            using var writer = new StreamWriter(deflateStream);
            writer.Write(json);
        }
        var base64 = Convert.ToBase64String(memoryStream.ToArray());
        return Uri.EscapeDataString(base64);
    }
    
    public SolutionModel? DecodeSolution(string encodedBase64)
    {
        try
        {
            var base64 = Uri.UnescapeDataString(encodedBase64);
            var compressedBytes = Convert.FromBase64String(base64);
            using var memoryStream = new MemoryStream(compressedBytes);
            using var deflateStream = new DeflateStream(memoryStream, CompressionMode.Decompress);
            using var reader = new StreamReader(deflateStream);
            var json = reader.ReadToEnd();
            return JsonSerializer.Deserialize<SolutionModel>(json, _options);
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return null;
        }
    }
}