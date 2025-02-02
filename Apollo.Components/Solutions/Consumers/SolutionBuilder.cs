using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;

namespace Apollo.Components.Solutions.Consumers;

public class SolutionBuilder : IConsumer<BuildSolution>
{
    private readonly SolutionsState _state;

    public SolutionBuilder(SolutionsState state)
    {
        _state = state;
    }

    public async Task Consume(BuildSolution message)
    {
        await _state.BuildAsync();
    }
}