using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Extensions;

public class KeyboardShortcut
{
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public bool Meta { get; set; }
    public KeyCode Key { get; set; }

    public static KeyboardShortcut FromKeyCodeArgs(FluentKeyCodeEventArgs args) => new()
    {
        Ctrl = args.CtrlKey,
        Alt = args.AltKey,
        Shift = args.ShiftKey,
        Meta = args.MetaKey,
        Key = args.Key
    };

    public string GetDisplayText(KeyCode key)
    {
        var text = key.GetDisplayName();
        return text.StartsWith("Key") ? text[3..] : text;
    }
} 