using Apollo.Components.Infrastructure;
using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Events;
using Apollo.Components.Solutions.Services;

namespace Apollo.Components.Solutions.Consumers;

public class BuildRequestSolutionSaver : IConsumer<BuildRequested>
{
    private readonly ISolutionSaveService _saveService;

    public BuildRequestSolutionSaver(ISolutionSaveService saveService)
    {
        _saveService = saveService;
    }

    public async Task Consume(BuildRequested message)
    {
        await _saveService.SaveSolutionAsync(message.Solution);
    }
}