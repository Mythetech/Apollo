using Apollo.Contracts.Workers;
using Apollo.Infrastructure.Workers;

namespace Apollo.Test.Workers;

public class TestLoggerProxy : ILoggerProxy
{
    public List<LogMessage> Logs { get; } = [];
    
    public void Log(string message, LogSeverity severity)
    {
        Logs.Add(new LogMessage(message, severity, DateTimeOffset.Now));
    }
}