using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Shared.ApolloNotificationBar;
using Apollo.Components.Solutions.Commands;
using Apollo.Components.Solutions.Services;
using Apollo.Contracts.Solutions;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Apollo.Components.Solutions.Consumers;

public class OpenBase64SolutionPrompter : IConsumer<PromptOpenBase64Solution>
    {
        private readonly IDialogService _dialogService;
        private readonly Base64Service _base64Service;
        private readonly SolutionsState _state;
        private readonly ISnackbar _snackbar;

        public OpenBase64SolutionPrompter(
            IDialogService dialogService,
            Base64Service base64Service,
            SolutionsState state,
            ISnackbar snackbar)
        {
            _dialogService = dialogService;
            _base64Service = base64Service;
            _state = state;
            _snackbar = snackbar;
        }

        public async Task Consume(PromptOpenBase64Solution message)
        {
            var options = new DialogOptions
            {
                MaxWidth = MaxWidth.Large,
                CloseButton = true,
            };

            var parameters = new DialogParameters()
            {
                ["EncodedSolution"] = message.Base64
            };
            
            var dialog = await _dialogService.ShowAsync<Base64OpenDialog>("Open Base64 Solution", parameters, options);
            await dialog.Result;
        }
    }
