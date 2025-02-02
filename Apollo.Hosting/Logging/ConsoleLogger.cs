namespace Apollo.Hosting.Logging;

public class ConsoleLogger : IHostLogger
{
    private readonly Action<string> _logCallback;

    public ConsoleLogger(Action<string> logCallback)
    {
        _logCallback = logCallback;
    }

    public void LogTrace(string message) => _logCallback($"[Trace] {message}");
    public void LogDebug(string message) => _logCallback($"[Debug] {message}");
    public void LogInformation(string message) => _logCallback($"[Info] {message}");
    public void LogWarning(string message) => _logCallback($"[Warn] {message}");
    public void LogError(string message) => _logCallback($"[Error] {message}");
} 