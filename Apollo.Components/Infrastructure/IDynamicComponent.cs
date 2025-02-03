namespace Apollo.Components.Infrastructure;

/// <summary>
/// Represents the contract for a dynamic blazor component that can be instantiated through DynamicComponent
/// </summary>
public interface IDynamicComponent
{
    /// <summary>
    /// Type of the blazor component
    /// </summary>
    public Type ComponentType { get; set; }
    
    /// <summary>
    /// Dictionary of parameters to pass to the dynamic component
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; }  
}