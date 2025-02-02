namespace Apollo.Components;

using Apollo.Components.Settings;

public class AppState
{
    public event Action AppStateChanged;
    private readonly SettingsState _settings;

    public AppState(SettingsState settings)
    {
        _settings = settings;
        _settings.SettingsChanged += HandleSettingsChanged;
    }

    public bool IsDarkMode => _settings.IsDarkMode;

    public void SetDarkMode()
    {
        _settings.ThemeMode = ThemeMode.Dark;
    }

    public void SetLightMode()
    {
        _settings.ThemeMode = ThemeMode.Light;
    }

    private void HandleSettingsChanged()
    {
        AppStateChanged?.Invoke();
    }
}