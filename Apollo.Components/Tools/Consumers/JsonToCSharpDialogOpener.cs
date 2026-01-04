using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Tools.Commands;
using MudBlazor;

namespace Apollo.Components.Tools.Consumers;

public sealed class JsonToCSharpDialogOpener : IConsumer<OpenJsonToCSharpDialog>
{
    private readonly IDialogService _dialogService;

    public JsonToCSharpDialogOpener(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task Consume(OpenJsonToCSharpDialog message)
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            NoHeader = true,
            MaxWidth = MaxWidth.ExtraLarge,
            FullWidth = true,
        };

        await _dialogService.ShowAsync<JsonToCSharpDialog>("JSON to C# Converter", options);
    }
}

