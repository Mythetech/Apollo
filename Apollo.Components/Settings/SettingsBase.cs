using System.Reflection;

namespace Apollo.Components.Settings;

public abstract class SettingsBase
{
    public virtual string Section { get; }
    
    public virtual Type Type { get; }
    
    public virtual object Model { get; }
}