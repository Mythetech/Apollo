using Apollo.Components.Infrastructure;

namespace Apollo.Components.DynamicTabs;

public abstract class DynamicTabView : ApolloBaseComponent, IDynamicComponent
{
    public virtual Guid TabId { get; set; } = Guid.NewGuid();
    
    public abstract string Name { get; set; }

    public virtual string AreaIdentifier { get; set; } = "";
    
    public int AreaIndex { get; set; } = 0;

    public int? BadgeCount { get; set; }

    public bool IsActive { get; set; } = false;
    
    public abstract Type ComponentType { get; set; }
    public virtual Dictionary<string, object> Parameters { get; set; } = new();

    public virtual string DefaultArea => DropZones.None;
}