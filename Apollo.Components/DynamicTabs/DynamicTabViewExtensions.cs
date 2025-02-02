
namespace Apollo.Components.DynamicTabs;

public static class DynamicTabViewExtensions
{
    public static SerializedDynamicTabModel ToSerializedModel(this DynamicTabView tab)
    {
        tab.Parameters.Remove("Instance");
        
        return new SerializedDynamicTabModel
        {
            TabId = tab.TabId,
            Name = tab.Name,
            AreaIdentifier = tab.AreaIdentifier,
            AreaIndex = tab.AreaIndex,
            BadgeCount = tab.BadgeCount,
            IsActive = tab.IsActive,
            ComponentType = tab.ComponentType.AssemblyQualifiedName,
            Parameters = tab.Parameters
        };
    }

    public static DynamicTabView ToModel(this SerializedDynamicTabModel model)
    {
        var type = Type.GetType(model.ComponentType);
        if (type == null)
            throw new InvalidOperationException($"Type {model.ComponentType} could not be found.");

        if (Activator.CreateInstance(type) is not DynamicTabView tab)
            throw new InvalidOperationException($"Type {model.ComponentType} is not a DynamicTabView.");

        tab.TabId = model.TabId;
        tab.Name = model.Name;
        tab.AreaIdentifier = model.AreaIdentifier;
        tab.AreaIndex = model.AreaIndex;
        tab.BadgeCount = model.BadgeCount;
        tab.IsActive = model.IsActive;
        tab.Parameters = model.Parameters;

        tab.Parameters["Instance"] = tab;
        
        return tab;
    }
}