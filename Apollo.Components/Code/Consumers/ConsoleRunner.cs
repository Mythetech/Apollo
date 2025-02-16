using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions;
using Apollo.Components.Solutions.Commands;
using Apollo.Contracts.Solutions;

namespace Apollo.Components.Code.Consumers;

public class ConsoleRunner : IConsumer<RunSolution>
{
    private readonly CompilerState _compiler;
    private readonly IMessageBus _bus;
    private readonly SolutionsState _state;

    public ConsoleRunner(CompilerState compiler, IMessageBus bus, SolutionsState state)
    {
        _compiler = compiler;
        _bus = bus;
        _state = state;
    }

    public async Task Consume(RunSolution message)
    {
        var solution = message.Solution ?? _state.Project;
        
        if (solution.ProjectType != ProjectType.Console)
            return;

        await _bus.PublishAsync(new FocusTab("Console Output"));
        await _compiler.ExecuteAsync(solution);
    }
} 