using System.Text.Json;
using Apollo.Contracts.Workers;

namespace Apollo.Infrastructure.Workers;

public static class WorkerLogWriter
{
    public static string Log(string message, LogSeverity severity)
    {
        var log = new LogMessage(message, severity, DateTimeOffset.UtcNow);
        var workerMsg = new WorkerMessage()
        {
            Action = StandardWorkerActions.Log,
            Payload = JsonSerializer.Serialize(log)
        };
        
        return JsonSerializer.Serialize(workerMsg);
    }
    
    public static string InformationMessage(string message)
        => Log(message, LogSeverity.Information);
    
    public static string WarningMessage(string message)
        => Log(message, LogSeverity.Warning);
    
    public static string ErrorMessage(string message)
     => Log(message, LogSeverity.Error);
    
    public static string DebugMessage(string message)
     => Log(message, LogSeverity.Debug);
}