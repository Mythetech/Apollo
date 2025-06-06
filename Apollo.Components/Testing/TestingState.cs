using System.Diagnostics;
using System.Reflection;
using Apollo.Components.Code;
using Apollo.Components.Console;
using Apollo.Components.Solutions;
using System.Linq;

namespace Apollo.Components.Testing;

public class TestingState
{
    private readonly CompilerState _compiler;
    private readonly SolutionsState _solutions;
    private readonly TestConsoleOutputService _console;
    private Assembly? _cachedBuild;

    public event Action TestCasesStateChanged;
    
    private void NotifyTestStateChanged() => TestCasesStateChanged?.Invoke();
    
    public List<TestCase> TestCases { get; private set; } = new List<TestCase>();

    public TestingState(CompilerState compiler, SolutionsState solutions, TestConsoleOutputService console)
    {
        _compiler = compiler;
        _solutions = solutions;
        _console = console;
    }

    public void CacheBuild(Assembly assembly)
    {
        _cachedBuild = assembly;
    }

    public async Task<List<TestCase>> RediscoverTestsAsync()
    {
        return await DiscoverTestsAsync();
    }

    public async Task<List<TestCase>> DiscoverTestsAsync()
    {
        if (_cachedBuild == null)
        {
            return [];
        }
        var discoverer = new TestDiscoveryService(_console);
        TestCases = await discoverer.DiscoverTests(_cachedBuild);
        NotifyTestStateChanged();
        return TestCases;
    }

    public Task RunAllTestsAsync()
    {
        foreach (var test in TestCases)
        {
            RunTest(test);
            NotifyTestStateChanged();
        }

        return Task.CompletedTask;
    }
    
    public void RunTest(TestCase test)
    {
        if (test.Skip)
        {
            test.HasRun = true;
            test.Result = TestResult.Skipped;
            return;
        }
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var type = _cachedBuild.GetType(test.ClassName);
            if (type == null)
            {
                stopwatch.Stop();
                test.HasRun = true;
                test.Result = TestResult.Failed;
                test.ErrorMessage = $"Class '{test.ClassName}' not found.";
                test.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                return;
            }

            var method = type.GetMethod(test.MethodName);
            if (method == null)
            {
                stopwatch.Stop();
                test.HasRun = true;
                test.Result = TestResult.Failed;
                test.ErrorMessage = $"Method '{test.MethodName}' not found in class '{test.ClassName}'.";
                test.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                return;
            }

            object? instance = null;
            if (!method.IsStatic)
            {
                instance = Activator.CreateInstance(type);
                
                if (test.TestPlatform == "NUnit")
                {
                    var setupMethod = type.GetMethods()
                        .FirstOrDefault(m => m.GetCustomAttributes(typeof(NUnit.Framework.SetUpAttribute), true).Any());
                    setupMethod?.Invoke(instance, null);
                }
                
                if (instance == null)
                {
                    stopwatch.Stop();
                    test.HasRun = true;
                    test.Result = TestResult.Failed;
                    test.ErrorMessage = $"Failed to create an instance of '{test.ClassName}'.";
                    test.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                    return;
                }
            }
            
            _console.AddDebug($"Running test {test.MethodName}");

            method.Invoke(instance, null);

            stopwatch.Stop();
            _console.AddDebug($"Finished test {test.MethodName} in {stopwatch.ElapsedMilliseconds} ms.");
            _console.AddSuccess($"{test.MethodName} passed!");
            
            test.HasRun = true;
            test.Result = TestResult.Passed;
            test.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            _console.AddError($"{test.MethodName} failed {ex.InnerException?.Message ?? ex.Message}");
            test.HasRun = true;
            test.Result = TestResult.Failed;
            test.ErrorMessage = ex.InnerException?.Message ?? ex.Message;
        }
    }
}