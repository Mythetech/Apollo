using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;
using Apollo.Components.Solutions.Services;
using MudBlazor;

namespace Apollo.Components.Solutions.Consumers;

public class SaveSolutionAsPrompter : IConsumer<PromptSaveSolutionAs>
{
    private readonly IDialogService _dialogService;
    private readonly SolutionsState _state;

    public SaveSolutionAsPrompter(IDialogService dialogService, SolutionsState state)
    {
        _dialogService = dialogService;
        _state = state;
    }
    
    public async Task Consume(PromptSaveSolutionAs message)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            CloseOnEscapeKey = true
        };
        
        var dialogReference = await _dialogService.ShowAsync<SaveAsDialog>("Create New Solution", options);
        var result = await dialogReference.Result;
        
        if (result == null) return;

        if (!result.Canceled)
        {
           var solution = await _state.CreateNewSolutionAsync(result?.Data.ToString(), _state.Project.ProjectType, _state.Project.Items);
        }
    }
}