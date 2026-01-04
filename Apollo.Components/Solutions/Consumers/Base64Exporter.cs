using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Shared.ApolloNotificationBar;
using Apollo.Components.Solutions.Commands;
using Apollo.Components.Solutions.Services;
using MudBlazor;

namespace Apollo.Components.Solutions.Consumers;

public class Base64Exporter : IConsumer<ExportBase64String>
{
    private readonly SolutionsState _state;
    private readonly IJsApiService _jsApiService;
    private readonly Base64Service _base64Service;
    private readonly ISnackbar _snackbar;

    public Base64Exporter(SolutionsState state, IJsApiService jsApiService, Base64Service base64Service, ISnackbar snackbar)
    {
        _state = state;
        _jsApiService = jsApiService;
        _base64Service = base64Service;
        _snackbar = snackbar;
    }

    public async Task Consume(ExportBase64String message)
    {
        if (_state.Project == null)
            return;
        
        var encoded = _base64Service.EncodeSolution(_state.Project);
        await _jsApiService.CopyToClipboardAsync(encoded);
        _snackbar.AddApolloNotification("Base64 exported to clipboard");
    }
}