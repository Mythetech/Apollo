namespace Apollo.Hosting;

public class HostingConsoleService
{
    private readonly Action<string> _logCallback;

    public HostingConsoleService(Action<string> logCallback)
    {
        _logCallback = logCallback;
    }

    private void Log(string message, HostConsoleSeverity severity)
    {
        string msg = $"[{severity}] {message}";
        _logCallback.Invoke(msg);
    }

    public void LogTrace(string message) => Log(message, HostConsoleSeverity.Trace);
    public void LogDebug(string message) => Log(message, HostConsoleSeverity.Debug);
    public void LogInfo(string message) => Log(message, HostConsoleSeverity.Info);
    public void LogWarning(string message) => Log(message, HostConsoleSeverity.Warning);
    public void LogError(string message) => Log(message, HostConsoleSeverity.Error);
    public void LogSuccess(string message) => Log(message, HostConsoleSeverity.Success);
} 

public enum HostConsoleSeverity
{
    Debug,
    Trace,
    Info,
    Warning,
    Error,
    Success
}