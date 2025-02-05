using Apollo.Client.Code;
using Apollo.Components.Code;
using Apollo.Components.Debugging;
using KristofferStrube.Blazor.WebWorkers;
using Microsoft.JSInterop;

namespace Apollo.Client.Debugging;

public class DebuggerWorkerFactory : IDebuggerWorkerFactory
{
    private readonly IJSRuntime _jsRuntime;
    private readonly DebuggerConsole _console;

    public DebuggerWorkerFactory(IJSRuntime jsRuntime, DebuggerConsole console)
    {
        _jsRuntime = jsRuntime;
        _console = console;
    }
    
    public async Task<IDebuggerWorker> CreateAsync()
    {
        var worker = await SlimWorker.CreateAsync(_jsRuntime, "Apollo.Debugging.Worker", null);
        var proxy = new DebuggerWorkerProxy(worker, _jsRuntime, _console);
        await proxy.InitializeMessageListener();
        return proxy;
    }
}