using System.Reflection;
using Apollo.Components.Infrastructure.Environment;

namespace Apollo.Test.Components.Infrastructure;

public class TestingRuntimeEnvironment : IRuntimeEnvironment
{
    public string Name => "Testing";
    public Version Version => Assembly.GetExecutingAssembly().GetName().Version;
    public string BaseAddress => "localhost";
    
    public static TestingRuntimeEnvironment Instance => new();
}