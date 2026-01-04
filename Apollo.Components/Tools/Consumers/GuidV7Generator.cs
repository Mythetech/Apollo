using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Shared.ApolloNotificationBar;
using Apollo.Components.Tools.Commands;
using MudBlazor;

namespace Apollo.Components.Tools.Consumers;

public class GuidV7Generator : IConsumer<GenerateGuidV7>
{
    private readonly IJsApiService _jsApiService;
    private readonly ISnackbar _snackbar;

    public GuidV7Generator(IJsApiService jsApiService, ISnackbar snackbar)
    {
        _jsApiService = jsApiService;
        _snackbar = snackbar;
    }

    public async Task Consume(GenerateGuidV7 message)
    {
        var guid = Guid.CreateVersion7().ToString();
        await _jsApiService.CopyToClipboardAsync(guid);
        _snackbar.AddApolloNotification($"Copied {guid} to clipboard!", Severity.Success);
    }
}

