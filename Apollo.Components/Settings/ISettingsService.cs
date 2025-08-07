using Microsoft.AspNetCore.Components;

namespace Apollo.Components.Settings;

// ToDo figure out what this should be
public class ISettingsService<T> where T : SettingsBase 
{
    public ISettingsService(T model)
    {
        Model = model;
    }
    
    public T Model { get; }
}