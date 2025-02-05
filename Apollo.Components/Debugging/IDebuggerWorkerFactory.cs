namespace Apollo.Components.Debugging;

public interface IDebuggerWorkerFactory
{
    public Task<IDebuggerWorker> CreateAsync();

}