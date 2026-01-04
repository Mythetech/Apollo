using Apollo.Components.DynamicTabs;
using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Hosting.Events;
using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Solutions;

namespace Apollo.Components.DynamicClient.Consumers;

public class WebHostClientPreviewer : IConsumer<WebHostReady>
{
    private readonly IMessageBus _bus;
    private readonly SolutionsState _state;

    public WebHostClientPreviewer(IMessageBus bus, SolutionsState state)
    {
        _bus = bus;
        _state = state;
    }

    public async Task Consume(WebHostReady message)
    {
        await Task.Delay(1000);
        await Task.Yield();
        if (_state?.Project?.Files.Any(x => x.IsHtml() || x.IsRazor()) ?? false)
        {
            await _bus.PublishAsync(new UpdateTabLocationByName("Client Preview", DropZones.Floating));
        }
    }
}