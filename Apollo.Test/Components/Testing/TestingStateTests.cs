using System.Reflection;
using Apollo.Components.Code;
using Apollo.Components.Library.SampleProjects;
using Apollo.Components.Solutions;
using Apollo.Components.Testing;
using Apollo.Compilation;
using Apollo.Components.Analysis;
using Apollo.Components.Hosting;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Services;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using NSubstitute;
using NUnit.Framework.Internal;
using Xunit;
using TestResult = Apollo.Components.Testing.TestResult;

namespace Apollo.Test.Components.Testing;

public class TestingStateTests : ApolloBaseTestContext
{
    private readonly TestingState _testingState;
    private readonly CompilerState _compilerState;
    private readonly SolutionsState _solutionsState;
    private readonly TestConsoleOutputService _console;
    private Assembly _testAssembly;
    private IMessageBus _messageBus = new TestMessageBus();

    public TestingStateTests()
    {
        _console = new TestConsoleOutputService();
        _compilerState = new CompilerState(Substitute.For<ICompilerWorkerFactory>(),
            new ConsoleOutputService(JSInterop.JSRuntime, Substitute.For<IJsApiService>(), Substitute.For<IScrollManager>()),
            _messageBus,
            Substitute.For<ILogger<CompilerState>>(),
            Substitute.For<UserAssemblyStore>());
        _solutionsState = new SolutionsState(_compilerState, _messageBus, Substitute.For<ISolutionSaveService>(), Substitute.For<IHostingService>());
        _testingState = new TestingState(_compilerState, _solutionsState, _console);

        // Compile test assembly
        var compilationService = new CompilationService();
        var solution = TestingSampleProject.Create();
        var result = compilationService.Compile(solution.ToContract(), GetDefaultReferences());
        Assert.True(result.Success, "Test solution compilation failed");
        _testAssembly = Assembly.Load(result.Assembly!);
        
        _testingState.CacheBuild(_testAssembly);
    }

    [Fact]
    public async Task RunTest_SuccessfulTest_UpdatesTestCase()
    {
        // Arrange
        await _testingState.DiscoverTestsAsync();
        var test = _testingState.TestCases.First(t => 
            t.TestPlatform == "xUnit" && t.MethodName == "Add_ReturnsSum");

        // Act
        _testingState.RunTest(test);

        // Assert
        Assert.True(test.HasRun);
        Assert.Equal(TestResult.Passed, test.Result);
        Assert.Null(test.ErrorMessage);
        Assert.NotNull(test.ElapsedMilliseconds);
    }

    [Fact]
    public async Task RunTest_FailingTest_UpdatesTestCaseWithError()
    {
        // Arrange
        await _testingState.DiscoverTestsAsync();
        var test = _testingState.TestCases.First(t => 
            t.TestPlatform == "xUnit" && t.MethodName == "TestFail");

        // Act
        _testingState.RunTest(test);

        // Assert
        Assert.True(test.HasRun);
        Assert.Equal(TestResult.Failed, test.Result);
        Assert.NotNull(test.ErrorMessage);
        Assert.Contains("Assert.Equal() Failure", test.ErrorMessage);
    }

    [Fact]
    public async Task RunTest_SkippedTest_MarksAsSkipped()
    {
        // Arrange
        await _testingState.DiscoverTestsAsync();
        var test = _testingState.TestCases.First(t => 
            t.TestPlatform == "xUnit" && t.MethodName == "Subtract_ReturnsDifference_Skipped");

        // Act
        _testingState.RunTest(test);

        // Assert
        Assert.True(test.HasRun);
        Assert.Equal(TestResult.Skipped, test.Result);
        Assert.True(test.Skip);
        Assert.Equal("Not implemented", test.Metadata["SkipReason"]);
    }

    [Fact]
    public void RunTest_NonexistentClass_UpdatesTestCaseWithError()
    {
        // Arrange
        var test = new TestCase(
            "NonexistentClass.TestMethod",
            "xUnit",
            "NonexistentClass",
            "TestMethod",
            "NonexistentClass.TestMethod"
        );

        // Act
        _testingState.RunTest(test);

        // Assert
        Assert.True(test.HasRun);
        Assert.Equal(TestResult.Failed, test.Result);
        Assert.Contains("Class 'NonexistentClass' not found", test.ErrorMessage);
    }

    [Fact]
    public void RunTest_NonexistentMethod_UpdatesTestCaseWithError()
    {
        // Arrange
        var test = new TestCase(
            "CalculatorXunitTests.NonexistentMethod",
            "xUnit",
            "CalculatorXunitTests",
            "NonexistentMethod",
            "CalculatorXunitTests.NonexistentMethod"
        );

        // Act
        _testingState.RunTest(test);

        // Assert
        Assert.True(test.HasRun);
        Assert.Equal(TestResult.Failed, test.Result);
        Assert.Contains("Method 'NonexistentMethod' not found", test.ErrorMessage);
    }

    [Fact]
    public async Task RunAllTests_ExecutesAllTests()
    {
        // Arrange
        await _testingState.DiscoverTestsAsync();
        var initialTests = _testingState.TestCases.ToList();

        // Act
        await _testingState.RunAllTestsAsync();

        // Assert
        Assert.All(initialTests, test => Assert.True(test.HasRun));
        
        foreach (var test in initialTests)
        {
            _console.AddDebug($"Test {test.TestPlatform}.{test.MethodName}: {test.Result}");
        }

        var passedTests = initialTests.Count(t => t.Result == TestResult.Passed);
        var failedTests = initialTests.Count(t => t.Result == TestResult.Failed);
        var skippedTests = initialTests.Count(t => t.Result == TestResult.Skipped);
        
        Assert.True(initialTests.Any(t => 
            t.TestPlatform == "xUnit" && 
            t.MethodName == "Add_ReturnsSum" && 
            t.Result == TestResult.Passed), "xUnit Add_ReturnsSum should pass");

        Assert.True(initialTests.Any(t => 
            t.TestPlatform == "NUnit" && 
            t.MethodName == "Add_ReturnsSum" && 
            t.Result == TestResult.Passed), "NUnit Add_ReturnsSum should pass");

        Assert.Equal(2, passedTests);
        Assert.Equal(2, failedTests);  
        Assert.Equal(2, skippedTests); 
        Assert.Equal(6, initialTests.Count); 
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
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location)
        };

        return references;
    }
}