using System.Reflection;

namespace Apollo.Components.Testing;

public interface ITestDiscoveryService
{
    string Framework { get; }
    Task<List<TestCase>> DiscoverTestsAsync(Assembly assembly);
}