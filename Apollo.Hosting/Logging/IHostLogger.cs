namespace Apollo.Hosting.Logging;

public interface IHostLogger
{
    void LogTrace(string message);
    void LogDebug(string message);
    void LogInformation(string message);
    void LogWarning(string message);
    void LogError(string message);
} 