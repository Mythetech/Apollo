using Apollo.Hosting;
using Xunit;

namespace Apollo.Test.Hosting;

public class HostingConsoleServiceTests
{
    [Fact(DisplayName = "Appends logs of correct severity")]
    public void LogMethods_AppendCorrectSeverity()
    {
        // Arrange
        var logs = new List<string>();
        var console = new HostingConsoleService(logs.Add);

        // Act
        console.LogTrace("trace");
        console.LogDebug("debug");
        console.LogInfo("info");
        console.LogWarning("warning");
        console.LogError("error");

        // Assert
        Assert.Contains(logs, l => l == "[Trace] trace");
        Assert.Contains(logs, l => l == "[Debug] debug");
        Assert.Contains(logs, l => l == "[Info] info");
        Assert.Contains(logs, l => l == "[Warning] warning");
        Assert.Contains(logs, l => l == "[Error] error");
    }
}