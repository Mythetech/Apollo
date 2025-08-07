namespace Apollo.Components.Settings.Events;

public record SettingsModelChanged<T>(T Model) where T : SettingsBase;

public record SettingsModelChanged(Type Type, object Model);