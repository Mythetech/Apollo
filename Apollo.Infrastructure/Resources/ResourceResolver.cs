using System.Text.Json;

namespace Apollo.Infrastructure.Resources;

public class ResourceResolver : IResourceResolver
{
    private readonly HttpClient _httpClient;
    private readonly Lazy<Task<Dictionary<string, string>>> _resourceMappings;
    
    public ResourceResolver()
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(HostAddress.BaseUri);
        _resourceMappings = new Lazy<Task<Dictionary<string, string>>>(FetchResourcesAsync);
    }
    
    public ResourceResolver(string baseUri)
    {
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(baseUri);
        _resourceMappings = new Lazy<Task<Dictionary<string, string>>>(FetchResourcesAsync);
    }
    
    public async Task<string> ResolveResource(string logicalName)
    {
        if (string.IsNullOrWhiteSpace(logicalName))
            throw new ArgumentException("Logical name cannot be null or empty.", nameof(logicalName));

        var resources = await _resourceMappings.Value;

        if (resources.TryGetValue(logicalName, out var hashedName))
        {
            return hashedName;
        }

        throw new FileNotFoundException($"Resource '{logicalName}' not found in blazor.boot.json.");
    }

    private async Task<Dictionary<string, string>> FetchResourcesAsync()
    {
        var baseUri = GetBaseUri();
        if (baseUri.Contains("github", StringComparison.OrdinalIgnoreCase))
        {
            baseUri += "/Apollo";
        }
        var bootJsonUrl = $"{baseUri}/_framework/blazor.boot.json";
        var bootJsonContent = await _httpClient.GetStringAsync(bootJsonUrl);

        var bootJson = JsonSerializer.Deserialize<BlazorBootJson>(bootJsonContent);
        if (bootJson?.Resources?.Fingerprinting == null)
        {
            throw new InvalidOperationException("Invalid blazor.boot.json structure.");
        }

        var allResources = new Dictionary<string, string>();

        AddResources(bootJson.Resources.Fingerprinting);

        return allResources;

        void AddResources(Dictionary<string, string> resources)
        {
            if (resources == null) return;
            foreach (var resource in resources)
            {
                if (!allResources.ContainsKey(resource.Value))
                {
                    allResources.Add(resource.Value, resource.Key);
                }
            }
        }
    }

    private string GetBaseUri()
    {
        // Assume the base URI can be derived from the current environment or configuration.
        return _httpClient.BaseAddress.ToString().TrimEnd('/');
    }
}