using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Editor;
using Apollo.Components.Infrastructure.MessageBus;

namespace Apollo.Components.DynamicTabs.Consumers;

public class DockCloser : IConsumer<CloseDock>
{
    private readonly TabViewState _state;

    public DockCloser(TabViewState state)
    {
        _state = state;
    }
    
    public Task Consume(CloseDock message)
    {
        _state.CloseDock();
        return Task.CompletedTask;
    }
}