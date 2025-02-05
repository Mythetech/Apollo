using System.Text.Json;
using Apollo.Components.Console;
using Apollo.Components.Debugging;
using Apollo.Contracts.Debugging;
using Apollo.Contracts.Workers;
using Apollo.Infrastructure.Workers;
using KristofferStrube.Blazor.DOM;
using KristofferStrube.Blazor.WebWorkers;
using KristofferStrube.Blazor.Window;
using Microsoft.JSInterop;

namespace Apollo.Client.Debugging;

public class DebuggerWorkerProxy : IDebuggerWorker
{
    private readonly SlimWorker _worker;
    private readonly IJSRuntime _jsRuntime;
    private readonly DebuggerConsole _console;
    private readonly Dictionary<string, Delegate> _callbacks = new();

    protected internal DebuggerWorkerProxy(SlimWorker worker, IJSRuntime jsRuntime, DebuggerConsole console)
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
                case StandardWorkerActions.Log:
                    if (_callbacks.TryGetValue("log", out var logCallback) && logCallback is Func<LogMessage, Task> typedLogCallback)
                    {
                        var log = JsonSerializer.Deserialize<LogMessage>(message.Payload);
                        if (log != null)
                        {
                            await typedLogCallback.Invoke(log);
                        }
                    }
                    break;

                case StandardWorkerActions.Error:
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
    
    public bool IsDebugging { get; private set; }
    
    public bool IsPaused { get; private set; }
    
    public DebugLocation? CurrentLocation { get; }
    
    public event Action<DebuggerEvent>? OnDebugEvent;

    protected void NotifyDebugEvent(DebuggerEvent evt) => OnDebugEvent?.Invoke(evt);
    
    public async Task SetBreakpoint(Breakpoint breakpoint)
    {
        throw new NotImplementedException();
    }

    public async Task RemoveBreakpoint(Breakpoint breakpoint)
    {
        throw new NotImplementedException();
    }

    public async Task Continue()
    {
        throw new NotImplementedException();
    }

    public async Task StepOver()
    {
        throw new NotImplementedException();
    }

    public async Task Pause()
    {
        throw new NotImplementedException();
    }

    public async Task SendMessageAsync(WorkerMessage message)
    {
        Imports.PostMessage(JsonSerializer.Serialize(message));
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
}