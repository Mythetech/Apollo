namespace Apollo.Infrastructure.Resources;

public interface IResourceResolver
{
    public Task<string> ResolveResource(string resource);
}