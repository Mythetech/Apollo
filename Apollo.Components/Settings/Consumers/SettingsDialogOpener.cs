using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Settings.Commands;
using MudBlazor;

namespace Apollo.Components.Settings.Consumers;

public class SettingsDialogOpener : IConsumer<OpenSettingsDialog>
{
    private readonly IDialogService _dialogService;

    public SettingsDialogOpener(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }
    public async Task Consume(OpenSettingsDialog message)
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            NoHeader = true,
            MaxWidth = MaxWidth.Large,
        };
        
        await _dialogService.ShowAsync<SettingsDialog>("Settings", options);
    }
}