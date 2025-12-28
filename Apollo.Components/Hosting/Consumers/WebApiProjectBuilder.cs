using Apollo.Components.DynamicTabs;
using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions;
using Apollo.Components.Solutions.Commands;
using Apollo.Contracts.Solutions;

namespace Apollo.Components.Hosting.Consumers;

public class WebApiProjectBuilder : IConsumer<BuildSolution>
{
    private readonly IHostingService _hostingService;
    private readonly IMessageBus _bus;
    private readonly SolutionsState _state;

    public WebApiProjectBuilder(IHostingService hostingService, IMessageBus bus, SolutionsState state)
    {
        _hostingService = hostingService;
        _bus = bus;
        _state = state;
    }

    public async Task Consume(BuildSolution message)
    {
        var solution = message.Solution ?? _state.Project;
        
        if (solution.ProjectType != ProjectType.WebApi)
            return;

        await _bus.PublishAsync(new FocusTab("Web Host"));
        
        await _hostingService.RunAsync(solution);
    }
} 