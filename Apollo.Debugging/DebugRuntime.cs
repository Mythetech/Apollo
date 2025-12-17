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

    public static void CheckBreakpointAsync(string file, int line)
    {
        if (_state != DebuggerState.Running)
        {
            return;
        }

        _state = DebuggerState.Paused;
        _resumeTask = new TaskCompletionSource();
        
        _onBreakpoint?.Invoke(file, line);

        LogCallback?.Invoke("Debugger paused, waiting for resume...", LogSeverity.Trace);
        
        int iteration = 0;
        var lastCheckTime = DateTime.UtcNow;
        
        while (_state == DebuggerState.Paused)
        {
            iteration++;
            
            var now = DateTime.UtcNow;
            var elapsed = (now - lastCheckTime).TotalMilliseconds;
            
            if (elapsed > 5)
            {
                try
                {
                    Task.Delay(5).GetAwaiter().GetResult();
                }
                catch
                {
                }
                lastCheckTime = DateTime.UtcNow;
            }
            
            if (iteration % 5000 == 0)
            {
                LogCallback?.Invoke($"Still waiting for resume... (iteration {iteration}, state: {_state})", LogSeverity.Trace);
            }
        }
        
        LogCallback?.Invoke($"Resuming execution... (state changed to {_state})", LogSeverity.Trace);
    }

    public static void Continue()
    {
        LogCallback?.Invoke($"Continue() called, current state: {_state}", LogSeverity.Trace);
        if (_state == DebuggerState.Paused)
        {
            _state = DebuggerState.Running;
            _resumeTask?.SetResult();
            LogCallback?.Invoke("Debugger resumed - state set to Running", LogSeverity.Information);
        }
        else
        {
            LogCallback?.Invoke($"Continue() called but state is {_state}, not Paused", LogSeverity.Warning);
        }
    }

    public static void Pause()
    {
        _state = DebuggerState.Paused;
    }
    
    public static void Stop()
    {
        _state = DebuggerState.Running;
        _resumeTask?.SetResult();
    }
}

public enum DebuggerState
{
    Running,
    Paused
}