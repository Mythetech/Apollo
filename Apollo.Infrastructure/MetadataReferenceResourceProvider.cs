using Apollo.Infrastructure.Resources;
using Apollo.Infrastructure.Webcil;
using Microsoft.CodeAnalysis;

namespace Apollo.Infrastructure;

public class MetadataReferenceResourceProvider : IMetadataReferenceResolver
{
    private readonly string _baseUri;
    private readonly IResourceResolver _resourceResolver = new ResourceResolver();

    public MetadataReferenceResourceProvider()
    {
        _baseUri = HostAddress.BaseUri;
    }

    public MetadataReferenceResourceProvider(string baseUri = "https://localhost:7092")
    {
        _baseUri = baseUri;
    }

    public async Task<PortableExecutableReference> GetMetadataReferenceAsync(string wasmModule)
    {
        var httpClient = new HttpClient();
        httpClient.BaseAddress = new Uri(_baseUri);
        await using var stream = await httpClient.GetStreamAsync(await ResolveResourceStreamUri(wasmModule));
        var peBytes = WebcilConverterUtil.ConvertFromWebcil(stream);

        using var peStream = new MemoryStream(peBytes);
        return MetadataReference.CreateFromStream(peStream);
    }
    
    private async Task<string> ResolveResourceStreamUri(string resource)
    {
        string prefix = "/_framework";
        var resolved = await _resourceResolver.ResolveResource(resource);
        if (_baseUri.Contains("github", StringComparison.OrdinalIgnoreCase))
        {
            prefix = "/Apollo" + prefix;
        }
        return $"{prefix}/{resolved}";
    }
}