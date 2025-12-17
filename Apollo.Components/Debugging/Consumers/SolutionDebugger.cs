using Apollo.Components.Debugging.Commands;
using Apollo.Components.DynamicTabs.Commands;
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
    private readonly IMessageBus _messageBus;

    public SolutionDebugger(SolutionsState solutions, DebuggerState debugger, EditorState editor, IMessageBus messageBus)
    {
        _solutions = solutions;
        _debugger = debugger;
        _editor = editor;
        _messageBus = messageBus;
    }
    public async Task Consume(DebugSolution message)
    {
        var solution = message.Solution ?? _solutions.Project.ToContract();
        
        var breakpoints = _editor.Breakpoints
            .SelectMany(kvp => kvp.Value.Select(line => new Breakpoint(kvp.Key, line)))
            .ToList();
        
        await _messageBus.PublishAsync(new FocusTab("Debugging Output"));
        await Task.Delay(50);
        await _debugger.StartDebuggingAsync(solution, breakpoints);
    }
}