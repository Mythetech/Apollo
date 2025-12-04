namespace Apollo.Components.Settings.Events;

public record SettingsModelChanged<T> where T : SettingsBase
{
    public T Model { get; set; }
    public SettingsModelChanged()
    {
        
    }

    public SettingsModelChanged(T model)
    {
        Model = model;
    }
}

public record SettingsModelChanged(Type Type, object Model);