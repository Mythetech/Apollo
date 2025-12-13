using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Tools.Commands;
using MudBlazor;

namespace Apollo.Components.Tools.Consumers;

public sealed class EventViewerDialogOpener : IConsumer<OpenEventViewerDialog>
{
    private readonly IDialogService _dialogService;

    public EventViewerDialogOpener(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task Consume(OpenEventViewerDialog message)
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            NoHeader = true,
            MaxWidth = MaxWidth.ExtraLarge,
            FullWidth = true,
        };

        await _dialogService.ShowAsync<EventViewerDialog>("Event Viewer", options);
    }
}


