using System.Text;
using System.Text.Json;
using Apollo.Contracts.Compilation;
using Apollo.Contracts.Workers;
using Apollo.Infrastructure.Workers;
using KristofferStrube.Blazor.WebWorkers;

namespace Apollo.Compilation.Worker;

using System.IO;

class WorkerConsoleWriter : TextWriter
{
    public override Encoding Encoding => Encoding.UTF8;

    public override void WriteLine(string? value)
    {
        base.WriteLine(value);

        var logModel = new CompilerLog(value, LogSeverity.Information);
        // Post the log message to the main thread
        Imports.PostMessage(JsonSerializer.Serialize(new WorkerMessage
        {
            Action = "log",
            Payload = JsonSerializer.Serialize(logModel)
        }));
    }
}