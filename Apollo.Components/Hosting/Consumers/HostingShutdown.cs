using Apollo.Components.Hosting.Commands;
using Apollo.Components.Infrastructure.MessageBus;

namespace Apollo.Components.Hosting.Consumers;

public class HostingShutdown : IConsumer<Shutdown>
{
    private readonly IHostingService _hostingService;
    private readonly IMessageBus _bus;

    public HostingShutdown(IHostingService hostingService, IMessageBus bus)
    {
        _hostingService = hostingService;
        _bus = bus;
    }
    public async Task Consume(Shutdown message)
    {
        await _hostingService.StopAsync();
    }
}