using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Editor;
using Mythetech.Framework.Infrastructure.MessageBus;

namespace Apollo.Components.DynamicTabs.Consumers;

public class TabLocationUpdater : IConsumer<UpdateTabLocation>, IConsumer<UpdateTabLocationByName>
{
    private readonly TabViewState _state;

    public TabLocationUpdater(TabViewState state)
    {
        _state = state;
    }

    public Task Consume(UpdateTabLocation message)
    {
        var tab = _state.Tabs.FirstOrDefault(x => x.TabId.Equals(message.TabId));

        if (tab == null) return Task.CompletedTask;
        
        _state.UpdateTabLocation(tab, message.Location);
        return Task.CompletedTask;
    }

    public Task Consume(UpdateTabLocationByName message)
    {
        var tab = _state.Tabs.FirstOrDefault(x => x.Name.Equals(message.Name, StringComparison.OrdinalIgnoreCase));
        
        if (tab == null) return Task.CompletedTask;
        
        _state.UpdateTabLocation(tab, message.Location);
        return Task.CompletedTask;
    }
}