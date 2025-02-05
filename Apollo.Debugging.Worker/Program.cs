using System.Text.Json;
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
        await resolver.GetMetadataReferenceAsync("xunit.assert.wasm"),
        await resolver.GetMetadataReferenceAsync("xunit.core.wasm")
    };

    var service = new DebuggingService();
    
    await service.DebugAsync(debugMsg.Solution, references, logAction: LogCallback);

    var msg = new WorkerMessage()
    {
        Action = "debugging_completed",
        Payload = ""
    };

    Imports.PostMessage(JsonSerializer.Serialize(msg));
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
    WorkerLogWriter.InformationMessage(message);
}