using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Hosting.Commands;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Settings.Commands;
using Apollo.Components.Solutions.Commands;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Apollo.Components.Infrastructure.Keyboard;

public class KeyboardService : IDisposable
{
    private readonly IKeyCodeService _keyCodeService;
    private readonly IMessageBus _bus;
    private readonly KeyBindingsState _keyBindingsState;
    private bool _disposed;

    public KeyboardService(IKeyCodeService keyCodeService, IMessageBus bus, KeyBindingsState keyBindingsState)
    {
        _keyCodeService = keyCodeService;
        _bus = bus;
        _keyBindingsState = keyBindingsState;
    }

    public void RegisterShortcuts()
    {
        _keyCodeService.RegisterListener(HandleKeyDown);
    }

    private async Task HandleKeyDown(FluentKeyCodeEventArgs args)
    {
        foreach (var binding in _keyBindingsState.KeyBindings)
        {
            if (binding.Matches(args) && binding.Action != KeyBindingAction.None)
            {
                await ExecuteActionAsync(binding.Action);
                return;
            }
        }
    }

    private async Task ExecuteActionAsync(KeyBindingAction action)
    {
        switch (action)
        {
            case KeyBindingAction.BuildSolution:
                await _bus.PublishAsync(new BuildSolution());
                break;
            case KeyBindingAction.RunSolution:
                await _bus.PublishAsync(new RunSolution());
                break;
            case KeyBindingAction.SaveActiveSolution:
                await _bus.PublishAsync(new SaveActiveSolution());
                break;
            case KeyBindingAction.PromptSaveSolutionAs:
                await _bus.PublishAsync(new PromptSaveSolutionAs());
                break;
            case KeyBindingAction.OpenSettingsDialog:
                await _bus.PublishAsync(new OpenSettingsDialog());
                break;
            case KeyBindingAction.OpenAboutDialog:
                await _bus.PublishAsync(new OpenAboutDialog());
                break;
            case KeyBindingAction.FormatActiveDocument:
                await _bus.PublishAsync(new FormatActiveDocument());
                break;
            case KeyBindingAction.FormatAllDocuments:
                await _bus.PublishAsync(new FormatAllDocuments());
                break;
            case KeyBindingAction.PromptCreateNewFile:
                await _bus.PublishAsync(new PromptCreateNewFile());
                break;
            case KeyBindingAction.PromptCreateNewSolution:
                await _bus.PublishAsync(new PromptCreateNewSolution());
                break;
            case KeyBindingAction.PromptOpenFile:
                await _bus.PublishAsync(new PromptOpenFile());
                break;
            case KeyBindingAction.PromptOpenFolder:
                await _bus.PublishAsync(new PromptOpenFolder());
                break;
            case KeyBindingAction.PromptOpenGitHubRepo:
                await _bus.PublishAsync(new PromptOpenGitHubRepo());
                break;
            case KeyBindingAction.CloseSolution:
                await _bus.PublishAsync(new CloseSolution());
                break;
            case KeyBindingAction.CopyActiveFileToClipboard:
                await _bus.PublishAsync(new CopyActiveFileToClipboard());
                break;
            case KeyBindingAction.ExportBase64String:
                await _bus.PublishAsync(new ExportBase64String());
                break;
            case KeyBindingAction.ExportJsonFile:
                await _bus.PublishAsync(new ExportJsonFile());
                break;
            case KeyBindingAction.ExportZipFile:
                await _bus.PublishAsync(new ExportZipFile());
                break;
            case KeyBindingAction.ShareSolution:
                await _bus.PublishAsync(new ShareSolution());
                break;
            case KeyBindingAction.FocusTerminal:
                await _bus.PublishAsync(new FocusTab("Terminal"));
                break;
            case KeyBindingAction.FocusConsole:
                await _bus.PublishAsync(new FocusTab("Console"));
                break;
            case KeyBindingAction.FocusEditor:
                await _bus.PublishAsync(new FocusTab("Editor"));
                break;
            case KeyBindingAction.FocusSolutionExplorer:
                await _bus.PublishAsync(new FocusTab("Solution Explorer"));
                break;
            case KeyBindingAction.CloseDock:
                await _bus.PublishAsync(new CloseDock());
                break;
            case KeyBindingAction.OpenDock:
                await _bus.PublishAsync(new OpenDock());
                break;
            case KeyBindingAction.CloseAllFloatingWindows:
                await _bus.PublishAsync(new CloseAllFloatingWindows());
                break;
            case KeyBindingAction.StartRunning:
                await _bus.PublishAsync(new StartRunning());
                break;
            case KeyBindingAction.Shutdown:
                await _bus.PublishAsync(new Shutdown());
                break;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _keyCodeService.UnregisterListener(HandleKeyDown);
            _disposed = true;
        }
    }
}
