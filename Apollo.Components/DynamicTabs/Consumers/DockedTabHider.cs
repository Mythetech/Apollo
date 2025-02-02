using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Editor;
using Apollo.Components.Infrastructure.MessageBus;

namespace Apollo.Components.DynamicTabs.Consumers;

public class DockedTabHider : IConsumer<HideDockedTabs>
{
    private readonly TabViewState _state;

    public DockedTabHider(TabViewState state)
    {
        _state = state;
    }

    public async Task Consume(HideDockedTabs message)
    {
        _state.InactivateTabsInArea(DropZones.Docked);
    }
}