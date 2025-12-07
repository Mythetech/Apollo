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
    private readonly StringBuilder _buffer = new();
    
    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        if (value == '\n')
        {
            Flush();
        }
        else if (value != '\r')
        {
            _buffer.Append(value);
        }
    }

    public override void Write(string? value)
    {
        if (value == null) return;
        
        foreach (var c in value)
        {
            Write(c);
        }
    }

    public override void WriteLine(string? value)
    {
        Write(value);
        Flush();
    }

    public override void WriteLine()
    {
        Flush();
    }

    public override void Flush()
    {
        if (_buffer.Length == 0) return;
        
        var message = _buffer.ToString();
        _buffer.Clear();
        
        var logModel = new CompilerLog(message, LogSeverity.Information);
        Imports.PostMessage(JsonSerializer.Serialize(new WorkerMessage
        {
            Action = "log",
            Payload = JsonSerializer.Serialize(logModel)
        }));
    }
}