using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;
using MudBlazor;

namespace Apollo.Components.Solutions.Consumers;

public class CreateNewFilePrompter : IConsumer<PromptCreateNewFile>
{
    private readonly IDialogService _dialogService;
    private readonly SolutionsState _state;

    public CreateNewFilePrompter(IDialogService dialogService, SolutionsState state
        )
    {
        _dialogService = dialogService;
        _state = state;
    }
    public async Task Consume(PromptCreateNewFile message)
    {
        var options = new DialogOptions()
        {
            MaxWidth = MaxWidth.Medium,
            CloseOnEscapeKey = true
        };
        
        var dialog = await _dialogService.ShowAsync<AddFileDialog>($"Add File", options);
        var dialogResult = await dialog.Result;
        
        if(dialogResult == null || dialogResult.Canceled || dialogResult.Data == null)
            return;

        string fileName = (string)dialogResult.Data;
        _state.AddFile(fileName);
    }
}