using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.Shared.ApolloNotificationBar;
using Apollo.Components.Solutions.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Apollo.Components.Solutions.Commands;

public record ShareSolution();

public class ShareSolutionHandler : IConsumer<ShareSolution>
{
    private readonly SolutionsState _state;
    private readonly Base64Service _base64Service;
    private readonly ISnackbar _snackbar;
    private readonly NavigationManager _navigationManager;
    private readonly IJsApiService _jsApiService;

    public ShareSolutionHandler(
        SolutionsState state, 
        Base64Service base64Service,
        ISnackbar snackbar,
        NavigationManager navigationManager,
        IJsApiService jsApiService)
    {
        _state = state;
        _base64Service = base64Service;
        _snackbar = snackbar;
        _navigationManager = navigationManager;
        _jsApiService = jsApiService;
    }

    public async Task Consume(ShareSolution message)
    {
        if (_state.Project == null)
        {
            _snackbar.AddApolloNotification("No solution is currently open", Severity.Warning);
            return;
        }

        var solution = _state.Project;
        var base64 = _base64Service.EncodeSolution(solution);
        var url = $"{_navigationManager.BaseUri}?solution={base64}";

        await _jsApiService.CopyToClipboardAsync(url);
        _snackbar.AddApolloNotification("Solution sharing link copied to clipboard!", Severity.Success);
    }
}