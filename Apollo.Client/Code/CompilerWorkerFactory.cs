using Apollo.Components.Code;
using Apollo.Components.Console;
using KristofferStrube.Blazor.WebWorkers;
using Microsoft.JSInterop;

namespace Apollo.Client.Code;

public class CompilerWorkerFactory : ICompilerWorkerFactory
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ConsoleOutputService _console;

    public CompilerWorkerFactory(IJSRuntime jsRuntime, ConsoleOutputService console)
    {
        _jsRuntime = jsRuntime;
        _console = console;
    }
    
    public async Task<ICompilerWorker> CreateAsync()
    {
        var worker = await SlimWorker.CreateAsync(_jsRuntime, "Apollo.Compilation.Worker", null);
        var proxy = new CompilerWorkerProxy(worker, _jsRuntime, _console);
        await proxy.InitializeMessageListener();
        return proxy;
    }
}