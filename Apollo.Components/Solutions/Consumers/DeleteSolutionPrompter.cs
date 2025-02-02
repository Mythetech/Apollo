using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Shared.ApolloNotificationBar;
using Apollo.Components.Solutions.Commands;
using Apollo.Components.Solutions.Events;
using MudBlazor;

namespace Apollo.Components.Solutions.Consumers;

public class DeleteSolutionPrompter : IConsumer<PromptDeleteSolution>
{
    private readonly IDialogService _dialogService;
    private readonly SolutionsState _state;
    private readonly IMessageBus _bus;
    private readonly ISnackbar _snackbar;

    public DeleteSolutionPrompter(
        IDialogService dialogService,
        SolutionsState state,
        IMessageBus bus,
        ISnackbar snackbar)
    {
        _dialogService = dialogService;
        _state = state;
        _bus = bus;
        _snackbar = snackbar;
    }

    public async Task Consume(PromptDeleteSolution message)
    {
        if (!_state.HasActiveSolution)
            return;

        var result = await _dialogService.ShowMessageBox(
            "Delete Solution",
            $"Are you sure you want to delete solution '{_state.Project.Name}'? This cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");

        if (result == true)
        {
            var solutionName = _state.Project.Name;
            
            // Find next solution to switch to
            var nextSolution = _state.Solutions
                .Where(s => !s.Name.Equals(solutionName, StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
            
            // Close the current solution first
            await _bus.PublishAsync(new CloseSolution());
            
            // Remove from solutions list
            _state.Solutions.RemoveAll(s => s.Name.Equals(solutionName, StringComparison.OrdinalIgnoreCase));
            
            // Switch to next solution if available
            if (nextSolution != null)
            {
                _state.SwitchSolution(nextSolution.Name);
            }
            
            // Notify that solution was deleted
            await _bus.PublishAsync(new SolutionDeleted(solutionName));
            
            _snackbar.AddApolloNotification($"Solution '{solutionName}' was deleted", Severity.Success);
        }
    }
}