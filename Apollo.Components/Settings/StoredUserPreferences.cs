namespace Apollo.Components.Settings;

public record StoredUserPreferences(
    string? Theme = "Apollo", 
    ThemeMode? Mode = ThemeMode.Dark,
    string? CustomThemeId = null);

