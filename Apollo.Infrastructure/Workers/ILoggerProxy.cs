using Apollo.Contracts.Workers;

namespace Apollo.Infrastructure.Workers;

public interface ILoggerProxy
{
    public void Log(string message, LogSeverity severity);
}

public static class ILoggerProxyExtensions
{
    public static void Trace(this ILoggerProxy logger, string message)
        => logger.Log(message, LogSeverity.Trace);
    
    public static void LogDebug(this ILoggerProxy logger, string message)
     => logger.Log(message, LogSeverity.Debug);
    
    public static void LogTrace(this ILoggerProxy logger, string message)
        => logger.Log(message, LogSeverity.Trace);
    
    public static void LogInformation(this ILoggerProxy logger, string message)
        => logger.Log(message, LogSeverity.Information);
    
    public static void LogWarning(this ILoggerProxy logger, string message)
        => logger.Log(message, LogSeverity.Warning);
    
    public static void LogError(this ILoggerProxy logger, string message)
     => logger.Log(message, LogSeverity.Error);
}