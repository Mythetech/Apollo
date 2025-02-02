using System.Text.Json;
using Apollo.Contracts.Compilation;
using Apollo.Contracts.Workers;
using Apollo.Infrastructure.Workers;
using KristofferStrube.Blazor.WebWorkers;

namespace Apollo.Compilation.Worker;

public static class LogMessageWriter
{
    public static void Log(string message, LogSeverity severity = LogSeverity.Information)
    {
        var logModel = new CompilerLog(message, severity);
        Imports.PostMessage(JsonSerializer.Serialize(new WorkerMessage
        {
            Action = "log",
            Payload = JsonSerializer.Serialize(logModel)
        }));
    }
}