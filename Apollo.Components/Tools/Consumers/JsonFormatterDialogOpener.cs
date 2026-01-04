using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Tools.Commands;
using MudBlazor;

namespace Apollo.Components.Tools.Consumers;

public sealed class JsonFormatterDialogOpener : IConsumer<OpenJsonFormatterDialog>
{
    private readonly IDialogService _dialogService;

    public JsonFormatterDialogOpener(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task Consume(OpenJsonFormatterDialog message)
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            NoHeader = true,
            MaxWidth = MaxWidth.ExtraLarge,
            FullWidth = true,
        };

        await _dialogService.ShowAsync<JsonFormatterDialog>("JSON Formatter", options);
    }
}

