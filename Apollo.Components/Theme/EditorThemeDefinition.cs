using Apollo.Components.Settings;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Apollo.Components.Theme;

public abstract class EditorThemeDefinition
{
    protected SettingsState? AppState { get; private set; }
    
    public MudTheme BaseTheme { get; protected set; }

    public virtual string Name { get; protected set; } = "";

    public virtual bool HideAppIcon { get; protected set; } = false;

    public virtual string AppIconClass { get; set; } = "";
    
    public void Initialize(SettingsState appState)
    {
        AppState ??= appState;
    }
    
    public virtual string AppBarStyle => "background-color:var(--mud-palette-appbar-background)";

} 