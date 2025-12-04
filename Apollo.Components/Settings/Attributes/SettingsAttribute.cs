namespace Apollo.Components.Settings.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class SettingAttribute : Attribute
{
    public string Label { get; }
    public string? Description { get; set; }
    public int Order { get; set; } = 0;

    public SettingAttribute(string label)
    {
        Label = label;
    }
}