namespace Apollo.Components.DynamicTabs;

public class SerializedDynamicTabModel
{
    public Guid TabId { get; set; }
    public string Name { get; set; }
    public string AreaIdentifier { get; set; }
    public int AreaIndex { get; set; }
    public int? BadgeCount { get; set; }
    public bool IsActive { get; set; }
    public string ComponentType { get; set; } 
    public Dictionary<string, object> Parameters { get; set; }
}

