using Apollo.Components.Infrastructure;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Events;

namespace Apollo.Components.Solutions.Consumers;

public class SolutionDeletedHandler : IConsumer<SolutionDeleted>
{
    private readonly ISolutionSaveService _saveService;

    public SolutionDeletedHandler(ISolutionSaveService saveService)
    {
        _saveService = saveService;
    }

    public async Task Consume(SolutionDeleted message)
    {
        await _saveService.RemoveSolutionAsync(message.SolutionName);
    }
} 