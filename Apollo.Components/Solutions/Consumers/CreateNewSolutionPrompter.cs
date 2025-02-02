using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace Apollo.Components.Solutions.Consumers;

public class CreateNewSolutionPrompter : IConsumer<PromptCreateNewSolution>
{
    private readonly IDialogService _dialogService;

    public CreateNewSolutionPrompter(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }
    public async Task Consume(PromptCreateNewSolution message)
    {
        var parameters = new DialogParameters()
        {
            { "DefaultProjectType", message.DefaultProjectType }
        };
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            CloseOnEscapeKey = true
        };
        
        await _dialogService.ShowAsync<NewSolutionDialog>("Create New Solution", parameters, options);
    }
}