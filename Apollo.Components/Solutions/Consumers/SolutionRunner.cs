using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;

namespace Apollo.Components.Solutions.Consumers;

public class SolutionRunner : IConsumer<RunActiveSolution>
{
    private readonly SolutionsState _state;

    public SolutionRunner(SolutionsState state)
    {
        _state = state;
    }

    public async Task Consume(RunActiveSolution message)
    {
        await _state.BuildAndRunAsync();
    }
}