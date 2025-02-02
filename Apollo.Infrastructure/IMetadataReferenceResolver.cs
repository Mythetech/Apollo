using Microsoft.CodeAnalysis;

namespace Apollo.Infrastructure;

public interface IMetadataReferenceResolver
{
    Task<PortableExecutableReference> GetMetadataReferenceAsync(string wasmModule);
}