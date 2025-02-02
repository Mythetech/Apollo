using System.Reflection;

namespace Apollo.Components.Testing;

public interface ITestDiscoveryService
{
    public List<string> DiscoverTests(Assembly assembly);

}