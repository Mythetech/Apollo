using Microsoft.FluentUI.AspNetCore.Components;

namespace Apollo.Components.Infrastructure.Keyboard;

public static class DefaultKeyBindings
{
    public static List<KeyBinding> GetDefaults() =>
    [
        new KeyBinding
        {
            Id = "build",
            Ctrl = true,
            Key = KeyCode.KeyB,
            Action = KeyBindingAction.BuildSolution
        },
        new KeyBinding
        {
            Id = "run",
            Ctrl = true,
            Key = KeyCode.Enter,
            Action = KeyBindingAction.RunSolution
        },
        new KeyBinding
        {
            Id = "save",
            Ctrl = true,
            Key = KeyCode.KeyS,
            Action = KeyBindingAction.SaveActiveSolution
        },
        new KeyBinding
        {
            Id = "save-as",
            Ctrl = true,
            Shift = true,
            Key = KeyCode.KeyS,
            Action = KeyBindingAction.PromptSaveSolutionAs
        },
        new KeyBinding
        {
            Id = "format",
            Ctrl = true,
            Shift = true,
            Key = KeyCode.KeyF,
            Action = KeyBindingAction.FormatActiveDocument
        },
        new KeyBinding
        {
            Id = "settings",
            Ctrl = true,
            Key = KeyCode.Comma,
            Action = KeyBindingAction.OpenSettingsDialog
        },
        new KeyBinding
        {
            Id = "new-file",
            Ctrl = true,
            Key = KeyCode.KeyN,
            Action = KeyBindingAction.PromptCreateNewFile
        },
        new KeyBinding
        {
            Id = "focus-terminal",
            Ctrl = true,
            Shift = true,
            Key = KeyCode.IntlBackslash,
            Action = KeyBindingAction.FocusTerminal
        },
        new KeyBinding
        {
            Id = "close-dock",
            Ctrl = true,
            Shift = true,
            Key = KeyCode.Escape,
            Action = KeyBindingAction.CloseDock
        },
        new KeyBinding
        {
            Id = "close-floating",
            Meta = true,
            Shift = true,
            Key = KeyCode.KeyW,
            Action = KeyBindingAction.CloseAllFloatingWindows
        }
    ];
}

