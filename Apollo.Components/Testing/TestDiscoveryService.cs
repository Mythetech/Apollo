using System.Diagnostics;
using System.Reflection;
using Apollo.Components.Console;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using NullMessageSink = Xunit.Sdk.NullMessageSink;
using Apollo.Components.Testing.Providers;

namespace Apollo.Components.Testing;

public class TestDiscoveryService
{
    private readonly TestConsoleOutputService _console;
    private readonly IEnumerable<ITestDiscoveryService> _providers;

    public TestDiscoveryService(TestConsoleOutputService console)
    {
        _console = console;
        _providers = new ITestDiscoveryService[]
        {
            new XUnitTestProvider(console),
            new NUnitTestProvider(console)
        };
    }

    public async Task<List<TestCase>> DiscoverTests(Assembly assembly)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var discoveryTasks = _providers.Select(provider => provider.DiscoverTestsAsync(assembly));
        var testLists = await Task.WhenAll(discoveryTasks);
        
        var allTests = testLists.SelectMany(x => x).ToList();
        
        stopwatch.Stop();
        _console.AddDebug($"Discovered {allTests.Count} tests in {stopwatch.ElapsedMilliseconds}ms");

        return allTests;
    }
}
