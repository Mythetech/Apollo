using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Editor;
using Apollo.Components.Infrastructure.MessageBus;

namespace Apollo.Components.DynamicTabs.Consumers;

public class FloatingWindowCloser : IConsumer<CloseAllFloatingWindows>
{
    private readonly TabViewState _state;

    public FloatingWindowCloser(TabViewState state)
    {
        _state = state;
    }
    public Task Consume(CloseAllFloatingWindows message)
    {
        foreach (var tab in _state.Tabs.Where(x => x.AreaIdentifier.Equals(DropZones.Floating)))
        {
            _state.HideTab(tab.Name);
        }

        return Task.CompletedTask;
    }
}