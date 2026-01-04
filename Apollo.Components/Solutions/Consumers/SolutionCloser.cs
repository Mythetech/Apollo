using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;

namespace Apollo.Components.Solutions.Consumers;

public class SolutionCloser : IConsumer<CloseSolution>
{
    private readonly SolutionsState _state;

    public SolutionCloser(SolutionsState state)
    {
        _state = state;
    }

    public Task Consume(CloseSolution message)
    {
        _state.CloseActiveSolution();
        return Task.CompletedTask;
    }
}