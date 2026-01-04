using Apollo.Components.Editor;
using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Settings.Commands;
using MudBlazor;

namespace Apollo.Components.Settings.Consumers;

public class AboutDialogOpener : IConsumer<OpenAboutDialog>
{
    private readonly IDialogService _dialogService;

    public AboutDialogOpener(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task Consume(OpenAboutDialog message)
    {
        var options = new DialogOptions()
        {
            CloseOnEscapeKey = true,
            BackgroundClass = "bg-transparent",
            MaxWidth = MaxWidth.Large,
            CloseButton = true
        };
        
        await _dialogService.ShowAsync<AboutApolloDialog>("About", options);
    }
}