using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions;
using Apollo.Components.Solutions.Commands;
using Apollo.Contracts.Solutions;

namespace Apollo.Components.Code.Consumers;

public class LibraryProjectBuilder : IConsumer<BuildSolution>
{
    private readonly CompilerState _compiler;
    private readonly IMessageBus _bus;
    private readonly SolutionsState _state;

    public LibraryProjectBuilder(CompilerState compiler, IMessageBus bus, SolutionsState state)
    {
        _compiler = compiler;
        _bus = bus;
        _state = state;
    }

    public async Task Consume(BuildSolution message)
    {
        var solution = message.Solution ?? _state.Project;
        
        if (solution.ProjectType != ProjectType.ClassLibrary && 
            solution.ProjectType != ProjectType.RazorClassLibrary)
            return;

        await _bus.PublishAsync(new FocusTab("Library Output"));
        await _compiler.BuildAsync(solution);
    }
} 