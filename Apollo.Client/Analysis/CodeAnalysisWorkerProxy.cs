using System.Diagnostics;
using System.Text.Json;
using Apollo.Analysis;
using Apollo.Compilation;
using Apollo.Compilation.Worker;
using Apollo.Components.Analysis;
using Apollo.Components.Console;
using Apollo.Components.Solutions;
using Apollo.Contracts.Analysis;
using Apollo.Contracts.Workers;
using Apollo.Infrastructure.Workers;
using KristofferStrube.Blazor.DOM;
using KristofferStrube.Blazor.WebWorkers;
using KristofferStrube.Blazor.Window;
using Microsoft.JSInterop;
using OmniSharp.Models.Diagnostics;
using Solution = Microsoft.CodeAnalysis.Solution;


namespace Apollo.Client.Analysis;

public class CodeAnalysisWorkerProxy : ICodeAnalysisWorker, IWorkerProxy
{
    private readonly SlimWorker _worker;
    private readonly Dictionary<string, Delegate> _callbacks = new();
    private readonly IJSRuntime _jsRuntime;
    private readonly CodeAnalysisConsoleService _console;

    protected internal CodeAnalysisWorkerProxy(SlimWorker worker, IJSRuntime jsRuntime, CodeAnalysisConsoleService console)
    {
        _worker = worker;
        _jsRuntime = jsRuntime;
        _console = console;
    }
    
    internal async Task InitializeMessageListener()
{
    var eventListener = await EventListener<MessageEvent>.CreateAsync(_jsRuntime, async e =>
    {
        object? data = await e.Data.GetValueAsync();
        if (data is string json)
        {
            var message = JsonSerializer.Deserialize<WorkerMessage>(json);
            if (message == null)
            {
                return;
            }
            
            switch (message.Action)
            {
                case StandardWorkerActions.Log:
                    if (_callbacks.TryGetValue(StandardWorkerActions.Log, out var logCallback) && logCallback is Func<LogMessage, Task> typedLogCallback)
                    {
                        var log = JsonSerializer.Deserialize<LogMessage>(message.Payload);
                        if (log != null)
                        {
                            await typedLogCallback.Invoke(log);
                        }
                    }
                    break;

                case StandardWorkerActions.Error:
                    if (_callbacks.TryGetValue(StandardWorkerActions.Error, out var errorCallback) && errorCallback is Func<string, Task> typedErrorCallback)
                    {
                        var error = message.Payload;
                        if (error != null)
                        {
                            await typedErrorCallback.Invoke(error);
                        }
                    }
                    break;
                case "completion_response":
                    var bytes = Convert.FromBase64String(message.Payload);
                    if (bytes.Length > 0)
                    {
                        _response = bytes;
                    }

                    break;
                case "diagnostics_response":
                    _diagnosticsResponse = Convert.FromBase64String(message.Payload);
                    break;
                default:
                    Console.WriteLine($"Unknown event: {message.Action}", LogSeverity.Debug);
                    Console.WriteLine(JsonSerializer.Serialize(message.Payload));
                    break;
            }
        }
    });

    await _worker.AddOnMessageEventListenerAsync(eventListener);
}

    public void OnLog(Func<LogMessage, Task> callback)
    {
        _callbacks[StandardWorkerActions.Log] = callback;
    }

    public void OnError(Func<string, Task> callback)
    {
         _callbacks[StandardWorkerActions.Error] = callback;
    }

    public async Task TerminateAsync()
    {
        await _worker.DisposeAsync();
    }

    private byte[] _response = [];
    public async Task<byte[]> GetCompletionAsync(string code, string completionRequestString)
    {
        var msg = new WorkerMessage()
        {
            Action = "get_completion",
            Payload = JsonSerializer.Serialize(new CompletionRequestWrapper(code, completionRequestString))
        };

        _response = [];
        
        await _worker.PostMessageAsync(msg.ToSerialized());
        
        for(int i = 0; i < 50; i++) 
        {
            if(_response.Length < 1)
            {
                await Task.Delay(100);
            }
            else
            {
                return _response;
            }
        }

        return _response;
    }

    public async Task<byte[]> GetCompletionResolveAsync(string completionResolveRequestString)
    {
        throw new NotImplementedException();
    }

    public async Task<byte[]> GetSignatureHelpAsync(string code, string signatureHelpRequestString)
    {
        throw new NotImplementedException();
    }

    public async Task<byte[]> GetQuickInfoAsync(string quickInfoRequestString)
    {
        throw new NotImplementedException();
    }

    public async Task<byte[]> GetDiagnosticsAsync(string serializedSolution)
    {
        _diagnosticsResponse = null;
        
        await _worker.PostMessageAsync(JsonSerializer.Serialize(new WorkerMessage 
        {
            Action = "get_diagnostics",
            Payload = serializedSolution
        }));
        
        for(int i = 0; i < 50; i++) 
        {
            if(_diagnosticsResponse == null)
            {
                await Task.Delay(50);
                await Task.Yield();
            }
            else
            {
                return _diagnosticsResponse;
            }
        }

        return _diagnosticsResponse;
    }

    private byte[]? _diagnosticsResponse = null;
}