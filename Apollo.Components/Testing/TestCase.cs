using Microsoft.CodeAnalysis;

namespace Apollo.Components.Testing;

public class TestCase
{
    // Name of the test, e.g., "Namespace.ClassName.MethodName"
    public string Name { get; set; }

    // Full method name including namespace, class, and method
    public string FullyQualifiedName { get; set; }

    // Name of the class the test belongs to
    public string ClassName { get; set; }

    // Name of the method the test represents
    public string MethodName { get; set; }

    // Platform of the test, e.g., "xUnit", "NUnit", "MSTest"
    public string TestPlatform { get; set; }

    // Indicates whether the test has been run
    public bool HasRun { get; set; }

    // The result of the test
    public TestResult Result { get; set; }

    // Error message, if any (set during execution)
    public string? ErrorMessage { get; set; }
    
    public Dictionary<string, string?> Metadata { get; set; } = new(); 
    
    public long? ElapsedMilliseconds { get; set; }

    public bool Skip => Metadata.ContainsKey("SkipReason");

    // Constructor for convenience
    public TestCase(string name, string platform, string className, string methodName, string fullyQualifiedName)
    {
        Name = name;
        TestPlatform = platform;
        ClassName = className;
        MethodName = methodName;
        FullyQualifiedName = fullyQualifiedName;
        HasRun = false;
        Result = TestResult.NotRun;
    }
}

// Enum for test results
public enum TestResult
{
    NotRun,
    Passed,
    Failed,
    Skipped
}