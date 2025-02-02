using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;
using MudBlazor;

namespace Apollo.Components.Solutions.Consumers;

public class SaveSolutionAsPrompter : IConsumer<PromptSaveSolutionAs>
{
    private readonly IDialogService _dialogService;

    public SaveSolutionAsPrompter(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }
    public async Task Consume(PromptSaveSolutionAs message)
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            CloseOnEscapeKey = true
        };
        
        await _dialogService.ShowAsync<SaveAsDialog>("Create New Solution", options);
    }
}