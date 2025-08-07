using Apollo.Components.Settings;
using Apollo.Components.Settings.Attributes;

namespace Apollo.Components.Editor;

public class EditorSettings : SettingsBase
{
    [Setting(label: "Editor line height")]
    public int LineHeight { get; set; } = 24;

    public override string Section => "Editor";

    public override Type Type => typeof(EditorSettings);

    public override object Model => this;
}