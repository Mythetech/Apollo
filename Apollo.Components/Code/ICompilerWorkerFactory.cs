using Microsoft.JSInterop;

namespace Apollo.Components.Code;

public interface ICompilerWorkerFactory
{
    public Task<ICompilerWorker> CreateAsync();
}