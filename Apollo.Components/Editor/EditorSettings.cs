using Apollo.Components.Settings;
using Apollo.Components.Settings.Attributes;

namespace Apollo.Components.Editor;

public class EditorSettings : SettingsBase
{
    [Setting(label: "Font size")]
    public int FontSize { get; set; } = 14;
    
    [Setting(label: "Line height")]
    public int LineHeight { get; set; } = 24;
    
    [Setting(label: "Tab size")]
    public int TabSize { get; set; } = 4;
    
    [Setting(label: "Insert spaces (instead of tabs)")]
    public bool InsertSpaces { get; set; } = true;
    
    [Setting(label: "Word wrap")]
    public bool WordWrap { get; set; } = false;
    
    [Setting(label: "Show minimap")]
    public bool MinimapEnabled { get; set; } = true;
    
    [Setting(label: "Render whitespace")]
    public WhitespaceMode RenderWhitespace { get; set; } = WhitespaceMode.Selection;

    public override string Section => "Editor";
    public override Type Type => typeof(EditorSettings);
    public override object Model => this;
}

public enum WhitespaceMode
{
    None,
    Boundary,
    Selection,
    Trailing,
    All
}