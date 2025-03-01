using System.Reflection;
using Apollo.Components.Library.SampleProjects;
using Apollo.Components.Testing;
using Apollo.Components.Testing.Providers;
using Apollo.Compilation;
using Apollo.Components.Solutions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Apollo.Test.Components.Testing;

public class TestDiscoveryServiceTests : ApolloBaseTestContext
{
    private readonly CompilationService _compilationService;
    private readonly TestConsoleOutputService _console;
    private readonly TestDiscoveryService _discoveryService;
    private Assembly _testAssembly;

    public TestDiscoveryServiceTests()
    {
        _compilationService = new CompilationService();
        _console = new TestConsoleOutputService();
        _discoveryService = new TestDiscoveryService(_console);

        // Compile the test solution
        var solution = TestingSampleProject.Create();
        var result = _compilationService.Compile(solution.ToContract(), GetDefaultReferences());
        Assert.True(result.Success, "Test solution compilation failed");
        _testAssembly = Assembly.Load(result.Assembly!);
    }

    [Fact]
    public async Task DiscoverTests_FindsAllTests()
    {
        // Act
        var tests = await _discoveryService.DiscoverTests(_testAssembly);

        // Assert
        Assert.Equal(6, tests.Count);
    }

    [Fact]
    public async Task DiscoverTests_FindsXUnitTests()
    {
        // Act
        var tests = await _discoveryService.DiscoverTests(_testAssembly);
        var xunitTests = tests.Where(t => t.TestPlatform == "xUnit").ToList();

        // Assert
        Assert.Equal(3, xunitTests.Count);
        Assert.Contains(xunitTests, t => t.MethodName == "Add_ReturnsSum");
        Assert.Contains(xunitTests, t => t.MethodName == "Subtract_ReturnsDifference_Skipped");
    }

    [Fact]
    public async Task DiscoverTests_FindsNUnitTests()
    {
        // Act
        var tests = await _discoveryService.DiscoverTests(_testAssembly);
        var nunitTests = tests.Where(t => t.TestPlatform == "NUnit").ToList();

        // Assert
        Assert.Equal(3, nunitTests.Count);
        Assert.Contains(nunitTests, t => t.MethodName == "Add_ReturnsSum");
        Assert.Contains(nunitTests, t => t.MethodName == "Subtract_ReturnsDifference_Ignored");
    }

    [Fact]
    public async Task DiscoverTests_IdentifiesSkippedTests()
    {
        // Act
        var tests = await _discoveryService.DiscoverTests(_testAssembly);
        var skippedTests = tests.Where(t => t.Skip).ToList();

        // Assert
        Assert.Equal(2, skippedTests.Count);
        Assert.Contains(skippedTests, t => t.TestPlatform == "xUnit" && t.MethodName == "Subtract_ReturnsDifference_Skipped");
        Assert.Contains(skippedTests, t => t.TestPlatform == "NUnit" && t.MethodName == "Subtract_ReturnsDifference_Ignored");
    }

    private IEnumerable<MetadataReference> GetDefaultReferences()
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(FactAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("xunit.assert").Location),
            MetadataReference.CreateFromFile(typeof(NUnit.Framework.TestAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
        };

        return references;
    }
} 