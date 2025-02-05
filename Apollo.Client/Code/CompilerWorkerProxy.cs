using System.Text.Json;
using Apollo.Compilation;
using Apollo.Components.Code;
using Apollo.Components.Console;
using Apollo.Contracts.Compilation;
using Apollo.Contracts.Solutions;
using Apollo.Contracts.Workers;
using Apollo.Infrastructure.Workers;
using KristofferStrube.Blazor.DOM;
using KristofferStrube.Blazor.WebWorkers;
using KristofferStrube.Blazor.Window;
using Microsoft.JSInterop;

namespace Apollo.Client.Code;

public class CompilerWorkerProxy : ICompilerWorker
{
    private readonly SlimWorker _worker;
    private readonly Dictionary<string, Delegate> _callbacks = new();
    private readonly IJSRuntime _jsRuntime;
    private readonly ConsoleOutputService _console;

    protected internal CompilerWorkerProxy(SlimWorker worker, IJSRuntime jsRuntime, ConsoleOutputService console)
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
                _console.AddLog("Invalid event received - empty", ConsoleSeverity.Debug);
                return;
            }
            
            switch (message.Action)
            {
                case "log":
                    if (_callbacks.TryGetValue("log", out var logCallback) && logCallback is Func<LogMessage, Task> typedLogCallback)
                    {
                        var log = JsonSerializer.Deserialize<LogMessage>(message.Payload);
                        if (log != null)
                        {
                            await typedLogCallback.Invoke(log);
                        }
                    }
                    break;

                case "compile_completed":
                    if (_callbacks.TryGetValue("compile_completed", out var compileCallback) && compileCallback is Func<CompilationReferenceResult, Task> typedCompileCallback)
                    {
                        var compileResult = JsonSerializer.Deserialize<CompilationReferenceResult>(message.Payload);
                        if (compileResult != null)
                        {
                            await typedCompileCallback.Invoke(compileResult);
                        }
                    }
                    break;

                case "execute_completed":
                    if (_callbacks.TryGetValue("execute_completed", out var executeCallback) && executeCallback is Func<ExecutionResult, Task> typedExecuteCallback)
                    {
                        var executionResult = JsonSerializer.Deserialize<ExecutionResult>(message.Payload);
                        if(executionResult != null)
                            await typedExecuteCallback.Invoke(executionResult);
                    }
                    break;

                case "error":
                    if (_callbacks.TryGetValue("error", out var errorCallback) && errorCallback is Func<string, Task> typedErrorCallback)
                    {
                        var error = message.Payload;
                        if (error != null)
                        {
                            await typedErrorCallback.Invoke(error);
                        }
                    }
                    break;

                default:
                    _console.AddLog($"Unknown event: {message.Action}", ConsoleSeverity.Debug);
                    break;
            }
        }
    });

    await _worker.AddOnMessageEventListenerAsync(eventListener);
}
    
    public ICompilerWorker OnCompileCompleted(Func<CompilationReferenceResult, Task> callback)
    {
        _callbacks["compile_completed"] = callback;
        return this;
    }

    public ICompilerWorker OnExecuteCompleted(Func<ExecutionResult, Task> callback)
    {
        _callbacks["execute_completed"] = callback;
        return this;
    }

    public void OnLog(Func<LogMessage, Task> callback)
    {
        _callbacks["log"] = callback;
    }

    void IWorkerProxy.OnError(Func<string, Task> callback)
    {
        _callbacks["error"] = callback;
    }
    

    public async Task RequestBuildAsync(Solution solution)
    {
        var message = new WorkerMessage
        {
            Action = "compile",
            Payload = JsonSerializer.Serialize(solution)
        };
        await SendMessageAsync(message);
    }

    public async Task RequestExecuteAsync(byte[] assembly)
    {
        var message = new WorkerMessage
        {
            Action = "execute",
            Payload = assembly.Length > 0 ? Convert.ToBase64String(assembly) : ""
        };
        await SendMessageAsync(message);
    }

    public async Task SendMessageAsync(WorkerMessage message)
    {
        var payload = JsonSerializer.Serialize(message);
        await _worker.PostMessageAsync(payload);
    }

    public async Task TerminateAsync()
    {
        await _worker.DisposeAsync();
    }
}