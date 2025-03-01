using System.Reflection;

namespace Apollo.Components.Testing.Providers;

public class XUnitTestProvider : TestProviderBase
{
    public override string Framework => "xUnit";

    public XUnitTestProvider(TestConsoleOutputService console) : base(console)
    {
    }

    public override async Task<List<TestCase>> DiscoverTestsAsync(Assembly assembly)
    {
        var discoveredTests = new List<TestCase>();
        
        foreach (var type in assembly.GetTypes())
        {
            foreach (var method in type.GetMethods())
            {
                var factAttribute = method.GetCustomAttribute<Xunit.FactAttribute>();
                if (factAttribute != null)
                {
                    var test = CreateTestCase(
                        type.FullName ?? "UnknownClass",
                        method.Name,
                        factAttribute.Skip
                    );

                    discoveredTests.Add(test);
                    Console.AddDebug($"Discovered xUnit test: {test.Name}");
                }
            }
        }

        return discoveredTests;
    }
} 