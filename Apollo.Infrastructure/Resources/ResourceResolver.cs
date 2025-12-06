using System.Text.Json;
using System.Text.RegularExpressions;

namespace Apollo.Infrastructure.Resources;

public partial class ResourceResolver : IResourceResolver
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

        throw new FileNotFoundException($"Resource '{logicalName}' not found in dotnet.js");
    }

    private async Task<Dictionary<string, string>> FetchResourcesAsync()
    {
        var baseUri = GetBaseUri();
        if (baseUri.Contains("github", StringComparison.OrdinalIgnoreCase))
        {
            baseUri += "/Apollo";
        }
        var bootJsonUrl = $"{baseUri}/_framework/dotnet.js";
        var bootJsonContent = await _httpClient.GetStringAsync(bootJsonUrl);
        string content = BootJsonRegex().Match(bootJsonContent).Value;
        content = content.Substring(13, content.Length - 24);
        
        var bootJson = JsonSerializer.Deserialize<BlazorBootJson>(content);
        
        if (bootJson?.Resources?.Assembly.Length < 1)
        {
            throw new InvalidOperationException("Invalid dotnet.js resource structure.");
        }

        var allResources = new Dictionary<string, string>();

        AddResources(bootJson.Resources.Assembly.Concat(bootJson.Resources.CoreAssembly).ToArray());
        
        return allResources;

        void AddResources(AssemblyResource[] resources)
        {
            if (resources == null) return;
            foreach (var resource in resources)
            {
                if (!allResources.ContainsKey(resource.virtualPath))
                {
                    allResources.Add(resource.virtualPath, resource.name);
                }
            }
        }
    }

    private string GetBaseUri()
    {
        // Assume the base URI can be derived from the current environment or configuration.
        return _httpClient.BaseAddress.ToString().TrimEnd('/');
    }

    [GeneratedRegex(@"\*json-start\*/([\s\S]*?)/\*json-end\*")]
    private static partial Regex BootJsonRegex();
}