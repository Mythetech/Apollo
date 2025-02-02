using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Apollo.Compilation;
using Apollo.Compilation.Worker;
using Apollo.Contracts.Workers;
using Apollo.Infrastructure;
using Apollo.Infrastructure.Workers;
using KristofferStrube.Blazor.WebWorkers;
using Microsoft.CodeAnalysis;
using Solution = Apollo.Contracts.Solutions.Solution;

if (!OperatingSystem.IsBrowser())
    throw new PlatformNotSupportedException("Can only be run in the browser!");

LogMessageWriter.Log("Starting up worker...", LogSeverity.Debug);

bool keepRunning = true;

var resolver = new MetadataReferenceResourceProvider(HostAddress.BaseUri);

byte[]? asmCache = [];

ConcurrentBag<string> executionMessages = [];

Console.SetOut(new WorkerConsoleWriter());
Console.SetError(new WorkerConsoleWriter());

Imports.RegisterOnMessage(async e =>
{
    var payload = e.GetPropertyAsString("data");
    
    var message = JsonSerializer.Deserialize<WorkerMessage>(payload);
    try
    {
        switch (message.Action.ToLower())
        {
            case "compile":
                var solution = JsonSerializer.Deserialize<Solution>(message.Payload);
                var references = new List<MetadataReference>
                {
                    await resolver.GetMetadataReferenceAsync("System.Private.CoreLib.wasm"),
                    await resolver.GetMetadataReferenceAsync("System.Runtime.wasm"),
                    await resolver.GetMetadataReferenceAsync("System.Console.wasm"),
                    await resolver.GetMetadataReferenceAsync("xunit.assert.wasm"),
                    await resolver.GetMetadataReferenceAsync("xunit.core.wasm")
                };
                var result = new CompilationService().Compile(solution, references);

                asmCache = result.Assembly;

                var msg = new WorkerMessage()
                {
                    Action = "compile_completed",
                    Payload = JsonSerializer.Serialize(result)
                };

                Imports.PostMessage(JsonSerializer.Serialize(msg));
                break;

            case "execute":
                // Load assembly and run.
                byte[]? asm = Convert.FromBase64String(message.Payload);

                var assembly = Assembly.Load(asm.Length > 0 ? asm : asmCache);
                var service = new CompilationService();
                var executionResult = service.Execute(assembly, LogCallback, CancellationToken.None);
                executionResult.Messages = executionMessages.ToList();
                var executedMsg = new WorkerMessage()
                {
                    Action = "execute_completed",
                    Payload = JsonSerializer.Serialize(executionResult)
                };

                Imports.PostMessage(JsonSerializer.Serialize(executedMsg));
                executionMessages.Clear();
                break;
        }
    }
    catch (Exception ex)
    {
        var errMessage = new WorkerMessage()
        {
            Action = "error",
            Payload = ex.Message
        };
        
        Imports.PostMessage(JsonSerializer.Serialize(errMessage));
    }
});

LogMessageWriter.Log("Worker ready", LogSeverity.Debug);

while (keepRunning)
    await Task.Delay(100);

return;

void LogCallback(string message)
{
    executionMessages.Add(message);
}