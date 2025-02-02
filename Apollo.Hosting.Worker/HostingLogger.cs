using Apollo.Contracts.Workers;
using Apollo.Infrastructure.Workers;
using KristofferStrube.Blazor.WebWorkers;

namespace Apollo.Hosting.Worker;

public class HostingLogger : ILoggerProxy
{
    public void Log(string message, LogSeverity severity)
    {
        Imports.PostMessage(
            WorkerLogWriter.Log(message, severity)
        );
    }
}