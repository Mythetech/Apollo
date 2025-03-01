using System.Reflection;

namespace Apollo.Components.Testing;

public abstract class TestProviderBase : ITestDiscoveryService
{
    protected readonly TestConsoleOutputService Console;
    
    public abstract string Framework { get; }

    protected TestProviderBase(TestConsoleOutputService console)
    {
        Console = console;
    }

    public abstract Task<List<TestCase>> DiscoverTestsAsync(Assembly assembly);

    protected TestCase CreateTestCase(string className, string methodName, string? skipReason = null)
    {
        var test = new TestCase(
            $"{className}.{methodName}",
            Framework,
            className,
            methodName,
            $"{className}.{methodName}"
        );

        if (skipReason != null)
        {
            test.Result = TestResult.Skipped;
            test.Metadata["SkipReason"] = skipReason;
        }

        return test;
    }
} 