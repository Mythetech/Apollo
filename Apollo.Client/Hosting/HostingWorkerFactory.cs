using Apollo.Client.Code;
using Apollo.Components.Code;
using Apollo.Components.Console;
using Apollo.Components.Hosting;
using Apollo.Components.Solutions;
using Apollo.Hosting;
using KristofferStrube.Blazor.WebWorkers;
using Microsoft.JSInterop;

namespace Apollo.Client.Hosting;

public class HostingWorkerFactory : IHostingWorkerFactory
{
    private readonly IJSRuntime _jsRuntime;
    private readonly WebHostConsoleService _console;

    public HostingWorkerFactory(IJSRuntime jsRuntime, WebHostConsoleService console)
    {
        _jsRuntime = jsRuntime;
        _console = console;
    }
    
    public async Task<IHostingWorker> CreateAsync(CancellationToken cancellationToken)
    {
        var worker = await SlimWorker.CreateAsync(_jsRuntime, "Apollo.Hosting.Worker", null);
        var proxy = new HostingWorkerProxy(worker, _jsRuntime, _console);
        await proxy.InitializeMessageListener();
        return proxy;
    }
}