
using System.Diagnostics;
using System.Reflection;
using Apollo.Components.Console;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using NullMessageSink = Xunit.Sdk.NullMessageSink;

namespace Apollo.Components.Testing;

public class XunitTestDiscoveryService
{
    private readonly TestConsoleOutputService _console;

    public XunitTestDiscoveryService(TestConsoleOutputService console)
    {
        _console = console;
    }

    public List<TestCase> DiscoverTests(Assembly assembly)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var discoveredTests = new List<TestCase>();

        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods())
            {
                var isFact = method.GetCustomAttributes(typeof(FactAttribute), false).Any();
                if (isFact)
                {
                    var test = new TestCase(
                        $"{type.FullName}.{method.Name}",
                        "xunit",
                        type.FullName ?? "UnknownClass",
                        method.Name,
                        $"{type.FullName}.{method.Name}"
                    );

                    // Check for [Skip] or other metadata
                    var skipAttribute = method.GetCustomAttribute<FactAttribute>();
                    if (skipAttribute?.Skip != null)
                    {
                        test.Result = TestResult.Skipped;
                        test.Metadata["SkipReason"] = skipAttribute.Skip;
                    }

                    discoveredTests.Add(test);
                    _console.AddDebug("Discovered test: " + test.Name);
                }
            }
        }
        
        stopwatch.Stop();
        
        _console.AddDebug($"Discovered {discoveredTests.Count} tests in {stopwatch.ElapsedMilliseconds}ms");

        return discoveredTests;
    }
    /*
    public List<string> DiscoverTests(Assembly assembly)
    {
        System.Console.WriteLine("Starting test discovery...");

        // Wrap assembly
        var assemblyInfo = Reflector.Wrap(assembly);
        System.Console.WriteLine("Assembly wrapped.");

        using var testFramework = new XunitTestFramework(new NullMessageSink());
        System.Console.WriteLine("TestFramework created.");

        var discoverer = testFramework.GetDiscoverer(assemblyInfo);
        System.Console.WriteLine("Discoverer created.");

        var testCases = new List<ITestCase>();

        // Discover tests
        var sink = new DiscoveryEventSink();
        System.Console.WriteLine("Starting discovery...");
        discoverer.Find(false, sink, TestFrameworkOptions.ForDiscovery());
        System.Console.WriteLine("Discovery finished.");

        return testCases.Select(tc => tc.DisplayName).ToList();
    }
    */
}
