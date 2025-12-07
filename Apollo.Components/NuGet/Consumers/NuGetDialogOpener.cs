using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.NuGet.Commands;
using MudBlazor;

namespace Apollo.Components.NuGet.Consumers;

public class NuGetDialogOpener : IConsumer<OpenNuGetDialog>
{
    private readonly IDialogService _dialogService;

    public NuGetDialogOpener(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task Consume(OpenNuGetDialog message)
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            NoHeader = true,
            MaxWidth = MaxWidth.Large,
        };
        
        await _dialogService.ShowAsync<NuGetDialog>("NuGet Packages", options);
    }
}

