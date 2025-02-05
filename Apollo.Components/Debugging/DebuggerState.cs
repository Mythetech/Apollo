using Apollo.Components.Code;
using Apollo.Components.Console;
using Apollo.Components.Editor;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Contracts.Debugging;
using Apollo.Contracts.Solutions;
using Apollo.Contracts.Workers;

namespace Apollo.Components.Debugging;

public class DebuggerState
{
    private IDebuggerWorker? _worker = default!;
    private readonly IDebuggerWorkerFactory _workerFactory;
    private readonly DebuggerConsole _console;
    private readonly IMessageBus _messageBus;
    private readonly EditorState _editorState;

    public bool Disabled { get; private set; }
    public bool IsDebugging { get; private set; }
    public bool IsPaused { get; private set; }
    public DebugLocation? CurrentLocation { get; private set; }
    public Dictionary<string, List<Breakpoint>> Breakpoints { get; } = new();
    public Dictionary<string, string>? CurrentVariables { get; private set; }

    public event Action? DebuggerStateChanged;
    
    private void NotifyDebuggerStateChanged() => DebuggerStateChanged?.Invoke();

    private bool _workerReady = false;

    public DebuggerState(IDebuggerWorkerFactory workerFactory, DebuggerConsole console, IMessageBus messageBus, EditorState editorState)
    {
        _workerFactory = workerFactory;
        _console = console;
        _messageBus = messageBus;
        _editorState = editorState;
    }
    
    public async Task StartAsync()
    {
        if (Disabled || _worker != null)
            return;
        
        _worker = await _workerFactory.CreateAsync();

        _worker.OnError(HandleError);

        _worker.OnLog(HandleLog);
        
        _worker.OnDebugEvent += HandleDebugEvent;

        while (!_workerReady)
        {
            await Task.Yield();
            await Task.Delay(100);
        }

        NotifyDebuggerStateChanged();
    }

    private Task HandleError(string error)
    {
        _console.AddError(error);
        return Task.CompletedTask;
    }
    

    private Task HandleLog(LogMessage log)
    {
        if(!_workerReady && log.Message.StartsWith("Debugging worker ready"))
        {
            _workerReady = true;
            NotifyDebuggerStateChanged();
        }
        
        _console.AddLog(log.Message, (ConsoleSeverity)log.Severity);
        return Task.CompletedTask;
    }

    private void HandleDebugEvent(DebuggerEvent evt)
    {
        switch (evt.Type)
        {
            case DebugEventType.Started:
                IsDebugging = true;
                IsPaused = false;
                break;
            case DebugEventType.Paused:
                IsPaused = true;
                CurrentLocation = evt.Location;
                CurrentVariables = evt?.Variables;
                break;
            case DebugEventType.Resumed:
                IsPaused = false;
                break;
            case DebugEventType.Terminated:
                IsDebugging = false;
                IsPaused = false;
                CurrentLocation = null;
                CurrentVariables = null;
                break;
            case DebugEventType.Stepped:
                break;
            case DebugEventType.Error:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        NotifyDebuggerStateChanged();
    }

    public async Task ToggleBreakpoint(string file, int line)
    {
        var breakpoint = new Breakpoint(file, line);
        var command = new DebugCommand 
        { 
            Type = Breakpoints.ContainsKey(file) && Breakpoints[file].Any(b => b.Line == line)
                ? DebugCommandType.RemoveBreakpoint 
                : DebugCommandType.SetBreakpoint,
            Breakpoint = breakpoint
        };

        if (command.Type == DebugCommandType.RemoveBreakpoint)
        {
            await _worker.RemoveBreakpoint(command.Breakpoint);
        }
        else
        {
            await _worker.SetBreakpoint(command.Breakpoint);
        }
    }

    public async Task StartDebuggingAsync(Solution solution, Breakpoint? breakpoint) => 
        await _worker.DebugAsync(solution, breakpoint);
    
    public async Task Continue() =>
        await _worker.Continue();
/*
    public async Task StepOver() =>
        await _worker.SendMessageAsync(new DebugCommand { Type = DebugCommandType.StepOver });

    

    public async Task Stop() =>
        await _worker.SendMessageAsync(new DebugCommand { Type = DebugCommandType.Stop });
*/
}