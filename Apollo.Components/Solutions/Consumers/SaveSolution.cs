using Apollo.Components.Infrastructure;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;
using Apollo.Components.Solutions.Services;

namespace Apollo.Components.Solutions.Consumers;

public class SaveSolution : IConsumer<SaveActiveSolution>
{
    private readonly SolutionsState _state;
    private readonly ISolutionSaveService _saveService;

    public SaveSolution(SolutionsState state, ISolutionSaveService saveService)
    {
        _state = state;
        _saveService = saveService;
    }
   

    public async Task Consume(SaveActiveSolution message)
    {
        await _state.SaveProjectFilesAsync();
        await _saveService.SaveSolutionAsync(_state.Project);
    }
}
