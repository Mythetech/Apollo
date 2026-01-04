using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Editor;
using Mythetech.Framework.Infrastructure.MessageBus;

namespace Apollo.Components.DynamicTabs.Consumers;

public class DockOpener : IConsumer<OpenDock>
{
    private readonly TabViewState _state;

    public DockOpener(TabViewState state)
    {
        _state = state;
    }

    public async Task Consume(OpenDock message)
    {
        _state.OpenDock();
    }
}