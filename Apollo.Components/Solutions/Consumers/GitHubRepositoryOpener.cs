using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Apollo.Components.Infrastructure.MessageBus;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using Apollo.Components.Shared;
using Apollo.Components.Solutions.Commands;
using Apollo.Components.Solutions.Services;

namespace Apollo.Components.Solutions.Consumers
{
    public class GitHubRepositoryOpener : IConsumer<PromptOpenGitHubRepo>
    {
        private readonly IDialogService _dialogService;

        public GitHubRepositoryOpener(
            IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        public async Task Consume(PromptOpenGitHubRepo message)
        {
            var opts = new DialogOptions()
            {
                MaxWidth = MaxWidth.Large,
                CloseButton = true,
            };
            
            var dialog = await _dialogService.ShowAsync<GitHubOpenDialog>("Open GitHub Repository", opts);
            var result = await dialog.Result;
            
            if (result.Canceled)
                return;
        }
    }
} 