using Apollo.Contracts.Debugging;
using Apollo.Contracts.Solutions;
using Apollo.Contracts.Workers;

namespace Apollo.Components.Debugging;

public interface IDebuggerWorker : IWorkerProxy
{
    bool IsDebugging { get; }
    bool IsPaused { get; }
    DebugLocation? CurrentLocation { get; }
    
    event Action<DebuggerEvent> OnDebugEvent;

    Task DebugAsync(Solution solution, Breakpoint breakpoint);
    
    Task SetBreakpoint(Breakpoint breakpoint);
    Task RemoveBreakpoint(Breakpoint breakpoint);
    Task Continue();
    Task StepOver();
    Task Pause();
    
    Task<Dictionary<string, string>>? GetVariables() => Task.FromResult<Dictionary<string, string>?>(null);
    Task<string[]>? GetCallStack() => Task.FromResult<string[]?>(null);
}