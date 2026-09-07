using System.Reflection;
using Mythetech.Framework.Infrastructure.Environment;
using Mythetech.Framework.Infrastructure.Plugins;
using Assembly = System.Reflection.Assembly;

namespace Apollo.Test.Components.Infrastructure;

public class TestingRuntimeEnvironment : IRuntimeEnvironment
{
    public string Name => "Testing";
    public Version Version => Assembly.GetExecutingAssembly().GetName().Version;
    public string BaseAddress => "localhost";
    public Platform Platform => Platform.WebAssembly;
    
    public static TestingRuntimeEnvironment Instance => new();
}