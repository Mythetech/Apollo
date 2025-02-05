using Apollo.Contracts.Workers;

namespace Apollo.Debugging;

public static class DebugRuntime
{
    private static volatile DebuggerState _state = DebuggerState.Running;
    private static Action<string, int>? _onBreakpoint;
    private static TaskCompletionSource? _resumeTask;
    
    public static Thread? ManagedCurrentThread { get; set; }

    public static Action<string, LogSeverity>? LogCallback { get; set; }
    
    public static Action? StatusUpdateCallback { get; set; }

    public static DebuggerState State => _state;

    public static void Initialize(Action<string, int> breakpointHandler)
    {
        _onBreakpoint = breakpointHandler;
        _resumeTask = null;
        _state = DebuggerState.Running;
    }

    public static void CheckBreakpoint(string file, int line)
    {
        if (_state != DebuggerState.Running)
        {
            return;
        }


        _state = DebuggerState.Paused;
        _resumeTask = new TaskCompletionSource();
        _onBreakpoint?.Invoke(file, line);


        LogCallback?.Invoke("Debugger paused, waiting for resume...", LogSeverity.Trace);
        
        while (_state == DebuggerState.Paused)
        {
            Task.Delay(1000).Wait();
        }
    }

    public static void Continue()
    {
        _state = DebuggerState.Running;
        _resumeTask?.SetResult();
    }

    public static void Pause()
    {
            _state = DebuggerState.Paused;
    }
}

public enum DebuggerState
{
    Running,
    Paused
}