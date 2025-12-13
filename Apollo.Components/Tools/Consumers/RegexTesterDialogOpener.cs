using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Tools.Commands;
using MudBlazor;

namespace Apollo.Components.Tools.Consumers;

public sealed class RegexTesterDialogOpener : IConsumer<OpenRegexTesterDialog>
{
    private readonly IDialogService _dialogService;

    public RegexTesterDialogOpener(IDialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task Consume(OpenRegexTesterDialog message)
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            NoHeader = true,
            MaxWidth = MaxWidth.ExtraLarge,
            FullWidth = true,
            
        };

        await _dialogService.ShowAsync<RegexTesterDialog>("Regex Tester", options);
    }
}


