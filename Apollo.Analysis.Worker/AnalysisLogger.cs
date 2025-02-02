using Apollo.Contracts.Workers;
using Apollo.Infrastructure.Workers;
using KristofferStrube.Blazor.WebWorkers;

namespace Apollo.Analysis.Worker;

public class AnalysisLogger : ILoggerProxy
{
    public void Log(string message, LogSeverity severity)
    {
        Imports.PostMessage(
                WorkerLogWriter.Log(message, severity)
            );
    }
}