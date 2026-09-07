using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Components.Snackbar;
using Apollo.Components.Tools.Commands;
using MudBlazor;

namespace Apollo.Components.Tools.Consumers;

public class GuidGenerator : IConsumer<GenerateGuid>
{
    private readonly IJsApiService _jsApiService;
    private readonly ISnackbar _snackbar;

    public GuidGenerator(IJsApiService jsApiService, ISnackbar snackbar)
    {
        _jsApiService = jsApiService;
        _snackbar = snackbar;
    }

    public async Task Consume(GenerateGuid message)
    {
        var guid = Guid.NewGuid().ToString();
        await _jsApiService.CopyToClipboardAsync(guid);
        _snackbar.AddMythetechNotification($"Copied {guid} to clipboard!", Severity.Success);
    }
}

