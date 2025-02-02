using System.Text.Json;
using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions.Commands;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Apollo.Components.Infrastructure.Keyboard;

public class KeyboardService : IDisposable
{
    private readonly IKeyCodeService _keyCodeService;
    private readonly IMessageBus _bus;
    private bool _disposed;

    public KeyboardService(IKeyCodeService keyCodeService, IMessageBus bus)
    {
        _keyCodeService = keyCodeService;
        _bus = bus;
        
        //RegisterShortcuts();
    }

    public void RegisterShortcuts()
    {
        _keyCodeService.RegisterListener(HandleKeyDown);
    }

    private async Task HandleKeyDown(FluentKeyCodeEventArgs args)
    {
        if (args.CtrlKey && args.ShiftKey)
        {
            switch (args.Key)
            {
                case KeyCode.IntlBackslash:
                    await _bus.PublishAsync(new FocusTab("Terminal"));
                    break;
                case KeyCode.Escape:
                    await _bus.PublishAsync(new CloseDock());
                    break;
            }
            return;
        }

        if (args.MetaKey && args.ShiftKey)
        {
            switch (args.Key)
            {
                case KeyCode.KeyW:
                    await _bus.PublishAsync(new CloseAllFloatingWindows());
                    break;
            }
            return;
        }

        // Handle Ctrl-only combinations
        if (args.CtrlKey)
        {
            switch (args.Key)
            {
                case KeyCode.KeyB:
                    await _bus.PublishAsync(new BuildSolution());
                    break;
                case KeyCode.KeyS:
                    await _bus.PublishAsync(new SaveActiveSolution());
                    break;
                case KeyCode.Enter:
                    await _bus.PublishAsync(new RunSolution());
                    break;
            }
            return;
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