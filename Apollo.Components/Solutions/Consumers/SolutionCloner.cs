using Apollo.Components.Infrastructure;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;
using Apollo.Components.Solutions.Services;

namespace Apollo.Components.Solutions.Consumers;

public class SolutionCloner : IConsumer<SaveSolutionAs>
{
    private readonly SolutionsState _state;
    private readonly ISolutionSaveService _saveService;

    public SolutionCloner(SolutionsState state, ISolutionSaveService saveService)
    {
        _state = state;
        _saveService = saveService;
    }

    public async Task Consume(SaveSolutionAs message)
    {
        var newSolution = new SolutionModel
        {
            Name = message.NewName,
            ProjectType = _state.Project.ProjectType,
            Items = _state.Project.Items.Select(item => item.Clone()).ToList()
        };

        foreach (var item in newSolution.Items)
        {
            item.Uri = item.Uri.Replace(_state.Project.Name, message.NewName);
        }

        _state.Solutions.Add(newSolution);
        _state.SwitchSolution(message.NewName);

        await _saveService.SaveSolutionAsync(newSolution);
    }
} 