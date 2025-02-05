namespace Apollo.Contracts.Debugging;

public record DebuggerEvent(DebugEventType Type, DebugLocation? Location = null, Dictionary<string, string>? Variables = null);

public enum DebugEventType 
{
    Started,
    Paused,
    Resumed,
    Stepped,
    Terminated,
    Error
}

public record DebugLocation(string File, int Line, int Column);
public record Breakpoint(string File, int Line);

public record DebugCommand
{
    public required DebugCommandType Type { get; init; }
    public Breakpoint? Breakpoint { get; init; }
}

public enum DebugCommandType
{
    Start,
    Continue,
    StepOver,
    StepInto,
    StepOut,
    Pause,
    Stop,
    SetBreakpoint,
    RemoveBreakpoint
} 