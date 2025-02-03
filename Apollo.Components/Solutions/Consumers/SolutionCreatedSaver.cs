using Apollo.Components.Infrastructure;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Events;
using Apollo.Components.Solutions.Services;

namespace Apollo.Components.Solutions.Consumers;

public class SolutionCreatedSaver: IConsumer<SolutionCreated>
{
    private readonly ISolutionSaveService _saveService;

    public SolutionCreatedSaver(ISolutionSaveService saveService)
    {
        _saveService = saveService;
    }
    
    public async Task Consume(SolutionCreated message)
    {
        await _saveService.SaveSolutionAsync(message.Solution);
    }
    
}