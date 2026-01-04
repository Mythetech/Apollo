using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Editor;
using Mythetech.Framework.Infrastructure.MessageBus;

namespace Apollo.Components.DynamicTabs.Consumers;

public class TabHider: IConsumer<HideTab>
{
    private readonly TabViewState _viewState;

    public TabHider(TabViewState viewState)
    {
        _viewState = viewState;
    }

    public async Task Consume(HideTab message)
    {
        _viewState.HideTab(message.TabName);
    }
}