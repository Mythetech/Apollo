using Apollo.Components.Debugging.Commands;
using Apollo.Components.Editor;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions;
using Apollo.Contracts.Debugging;

namespace Apollo.Components.Debugging.Consumers;

public class SolutionDebugger : IConsumer<DebugSolution>
{
    private readonly SolutionsState _solutions;
    private readonly DebuggerState _debugger;
    private readonly EditorState _editor;

    public SolutionDebugger(SolutionsState solutions, DebuggerState debugger, EditorState editor)
    {
        _solutions = solutions;
        _debugger = debugger;
        _editor = editor;
    }
    public async Task Consume(DebugSolution message)
    {
        var solution = message.Solution ?? _solutions.Project.ToContract();
        var breakpoint = _editor.Breakpoints.FirstOrDefault();
        var bp = new Breakpoint(breakpoint.Key, breakpoint.Value.First());
        await _debugger.StartDebuggingAsync(solution, bp);
    }
}