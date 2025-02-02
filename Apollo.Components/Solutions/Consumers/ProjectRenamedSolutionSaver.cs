using Apollo.Components.Infrastructure;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Events;

namespace Apollo.Components.Solutions.Consumers;

public class ProjectRenamedSolutionSaver : IConsumer<ItemRenamed>
{
    private readonly ISolutionSaveService _saveService;
    private readonly SolutionsState _state;

    public ProjectRenamedSolutionSaver(ISolutionSaveService saveService, SolutionsState state)
    {
        _saveService = saveService;
        _state = state;
    }

    public async Task Consume(ItemRenamed message)
    {
        if (!message.ProjectRenamed)
            return;
        
        await _saveService.RemoveSolutionAsync(message.OldName);
        await _saveService.SaveSolutionAsync(_state.Project);
    }
}