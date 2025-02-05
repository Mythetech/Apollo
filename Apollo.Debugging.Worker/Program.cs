using System.Text.Json;
using Apollo.Contracts.Debugging;
using Apollo.Contracts.Workers;
using Apollo.Debugging;
using Apollo.Infrastructure;
using Apollo.Infrastructure.Workers;
using KristofferStrube.Blazor.WebWorkers;
using Microsoft.CodeAnalysis;

if (!OperatingSystem.IsBrowser())
    throw new PlatformNotSupportedException("Can only be run in the browser!");

Imports.PostMessage(
    WorkerLogWriter.DebugMessage("Starting up debugging worker...")
);

var resolver = new MetadataReferenceResourceProvider(HostAddress.BaseUri);

const bool keepRunning = true;

DebugRuntime.LogCallback += (s, e) =>
{
    Imports.PostMessage(
        WorkerLogWriter.Log(s, e)
    );
};

DebugRuntime.StatusUpdateCallback += () =>
{
    //Imports.PostMessage(WorkerLogWriter.Log("Status Update Check", LogSeverity.Trace));
};

Imports.RegisterOnMessage(async e =>
{
    var payload = e.GetPropertyAsString("data");
    
    var message = JsonSerializer.Deserialize<WorkerMessage>(payload);
    
    try
    {
        switch (message.Action.ToLower())
        {
            
            case WorkerActions.Debug:
                await HandleDebugMessage(message);
                break;
            case WorkerActions.Continue:
                HandleContinue();
                break;
            default:
                Imports.PostMessage(
                    WorkerLogWriter.WarningMessage("Unhandled action: " + message.Action)
                    );
                break;
        }
    }
    catch (Exception ex)
    {
        Imports.PostMessage(ErrorWriter.SerializeErrorToWorkerMessage(ex));
    }
});

Imports.PostMessage(
    WorkerLogWriter.DebugMessage("Debugging worker ready")
);

while (keepRunning)
    await Task.Delay(100);

return;


async Task HandleDebugMessage(WorkerMessage message)
{
    var debugMsg = JsonSerializer.Deserialize<StartDebuggingMessage>(message.Payload);
    var references = new List<MetadataReference>
    {
        await resolver.GetMetadataReferenceAsync("System.Private.CoreLib.wasm"),
        await resolver.GetMetadataReferenceAsync("System.Runtime.wasm"),
        await resolver.GetMetadataReferenceAsync("System.Console.wasm"),
        await resolver.GetMetadataReferenceAsync("System.Collections.wasm"),  
        await resolver.GetMetadataReferenceAsync("System.Linq.wasm"),       
        await resolver.GetMetadataReferenceAsync("System.Threading.wasm"),
        await resolver.GetMetadataReferenceAsync("Apollo.Debugging.wasm"),
        await resolver.GetMetadataReferenceAsync("xunit.assert.wasm"),
        await resolver.GetMetadataReferenceAsync("xunit.core.wasm")
    };
    
    Imports.PostMessage(
        WorkerLogWriter.Log("Resolved metadata references", LogSeverity.Trace)
    );

    var service = new DebuggingService();
    
    Imports.PostMessage(
        WorkerLogWriter.Log("Debugging service ready", LogSeverity.Trace)
    );

    try
    {
        Imports.PostMessage(
            WorkerLogWriter.Log("Starting debug process", LogSeverity.Trace)
        );
        
        await service.DebugAsync(debugMsg.Solution, references, logAction: LogCallback, debugAction: DebugEventCallback);
    }
    catch (Exception ex)
    {
        SendError(ex.Message);
    }

    var msg = new WorkerMessage()
    {
        Action = "debugging_completed",
        Payload = ""
    };

    Imports.PostMessage(JsonSerializer.Serialize(msg));
}

void HandleContinue()
{
    Imports.PostMessage(
        WorkerLogWriter.Log("Handling continue, debugger current state: " + DebugRuntime.State, LogSeverity.Trace)
    );
    
    DebugRuntime.Continue();
    
    Imports.PostMessage(
        WorkerLogWriter.Log("Handled continue, debugger current state: " + DebugRuntime.State, LogSeverity.Trace)
    );
}

void SendError(string errorMessage)
{
    var msg = new WorkerMessage
    {
        Action = StandardWorkerActions.Error,
        Payload = errorMessage
    };
    Imports.PostMessage(msg.ToSerialized());
}

void LogCallback(string message)
{
    Imports.PostMessage(
        WorkerLogWriter.InformationMessage(message)
    );
}

void DebugEventCallback(DebuggerEvent evt)
{
    var msg = new WorkerMessage()
    {
        Action = evt.Type.ToString(),
        Payload = JsonSerializer.Serialize(evt)
    };
    
    Imports.PostMessage(msg.ToSerialized());
}