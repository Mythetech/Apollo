using Apollo.Analysis;
using Apollo.Client.Code;
using Apollo.Components.Analysis;
using Apollo.Components.Code;
using KristofferStrube.Blazor.WebWorkers;
using Microsoft.JSInterop;

namespace Apollo.Client.Analysis;

public class CodeAnalysisWorkerFactory : ICodeAnalysisWorkerFactory
{
    private readonly IJSRuntime _jsRuntime;
    private readonly CodeAnalysisConsoleService _console;

    public CodeAnalysisWorkerFactory(IJSRuntime jSRuntime, CodeAnalysisConsoleService console)
    {
        _jsRuntime = jSRuntime;
        _console = console;
    }

    public async Task<ICodeAnalysisWorker> CreateAsync()
    {
        var worker = await SlimWorker.CreateAsync(_jsRuntime, "Apollo.Analysis.Worker", null);
        var proxy = new CodeAnalysisWorkerProxy(worker, _jsRuntime, _console);
        await proxy.InitializeMessageListener();
        return proxy;
    }
}