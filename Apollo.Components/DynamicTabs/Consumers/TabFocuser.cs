using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Editor;
using Mythetech.Framework.Infrastructure.MessageBus;

namespace Apollo.Components.DynamicTabs.Consumers;

public class TabFocuser : IConsumer<FocusTab>
{
    private readonly TabViewState _viewState;

    public TabFocuser(TabViewState viewState)
    {
        _viewState = viewState;
    }

    public Task Consume(FocusTab message)
    {
        _viewState.FocusTab(message.TabName);
        return Task.CompletedTask;
    }
}