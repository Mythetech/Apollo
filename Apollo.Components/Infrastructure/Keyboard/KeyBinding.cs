using Microsoft.FluentUI.AspNetCore.Components;
using Microsoft.FluentUI.AspNetCore.Components.Extensions;

namespace Apollo.Components.Infrastructure.Keyboard;

public class KeyBinding
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public bool Ctrl { get; set; }
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public bool Meta { get; set; }
    public KeyCode Key { get; set; }
    public KeyBindingAction Action { get; set; }

    public bool Matches(FluentKeyCodeEventArgs args)
    {
        return Ctrl == args.CtrlKey &&
               Alt == args.AltKey &&
               Shift == args.ShiftKey &&
               Meta == args.MetaKey &&
               Key == args.Key;
    }

    public string GetDisplayText()
    {
        var parts = new List<string>();
        
        if (Ctrl) parts.Add("Ctrl");
        if (Alt) parts.Add("Alt");
        if (Shift) parts.Add("Shift");
        if (Meta) parts.Add("⌘");
        
        var keyText = Key.GetDisplayName();
        if (keyText.StartsWith("Key"))
            keyText = keyText[3..];
        else if (keyText == "Enter")
            keyText = "↵";
        else if (keyText == "Escape")
            keyText = "Esc";
        
        parts.Add(keyText);
        
        return string.Join(" + ", parts);
    }

    public KeyBinding Clone() => new()
    {
        Id = Id,
        Ctrl = Ctrl,
        Alt = Alt,
        Shift = Shift,
        Meta = Meta,
        Key = Key,
        Action = Action
    };
}

