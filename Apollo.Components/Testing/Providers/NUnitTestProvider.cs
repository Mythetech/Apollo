using System.Reflection;

namespace Apollo.Components.Testing.Providers;

public class NUnitTestProvider : TestProviderBase
{
    public override string Framework => "NUnit";

    public NUnitTestProvider(TestConsoleOutputService console) : base(console)
    {
    }

    public override async Task<List<TestCase>> DiscoverTestsAsync(Assembly assembly)
    {
        var discoveredTests = new List<TestCase>();
        
        foreach (var type in assembly.GetTypes())
        {
            var isFixture = type.GetCustomAttributes(typeof(NUnit.Framework.TestFixtureAttribute), true).Any();
            
            if (isFixture)
            {
                foreach (var method in type.GetMethods())
                {
                    var testAttribute = method.GetCustomAttribute<NUnit.Framework.TestAttribute>();
                    if (testAttribute != null)
                    {
                        var ignoreAttribute = method.GetCustomAttribute<NUnit.Framework.IgnoreAttribute>();
                        
                        var test = CreateTestCase(
                            type.FullName ?? "UnknownClass",
                            method.Name,
                            ignoreAttribute?.Reason
                        );

                        discoveredTests.Add(test);
                        Console.AddDebug($"Discovered NUnit test: {test.Name}");
                    }
                }
            }
        }

        return discoveredTests;
    }
} 