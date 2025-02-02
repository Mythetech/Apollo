namespace Apollo.Components.Infrastructure;

public interface IDynamicComponent
{
    public Type ComponentType { get; set; }
    
    public Dictionary<string, object> Parameters { get; set; }  
}