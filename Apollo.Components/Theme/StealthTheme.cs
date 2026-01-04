using MudBlazor;

namespace Apollo.Components.Theme;

/// <summary>
/// Dark theme - modern Material 3 inspired design
/// </summary>
public class StealthTheme : EditorThemeDefinition
{
    public override string Name => "Stealth";

    public override bool HideAppIcon => true;

    public override string AppBarStyle => (AppState?.IsDarkMode ?? true)
        ? "background: linear-gradient(to right, var(--mud-palette-appbar-background) 0%, var(--mud-palette-primary-lighten) 50%, var(--mud-palette-appbar-background) 100%);"
        : "background: linear-gradient(to right, var(--mud-palette-primary) 0%, var(--mud-palette-primary-lighten) 20%, var(--mud-palette-background) 100%);";

    public StealthTheme()
    {
        BaseTheme = new()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = "#666666",
                PrimaryLighten = "#8C8C8C",
                Secondary = "#808080",
                Tertiary = "#737373",
                Surface = "#FFFFFF",
                Background = "#FAFAFA",
                AppbarBackground = "#404040",
                AppbarText = "#FFFFFF",
                Dark = "#fdfafaee",
                Divider = "#737373",
                GrayDarker = "#fafafaeb",
                Success = "#4CAF50",
                Warning = "#FF9800",
                Error = "#F44336",
                Info = "#2196F3"
            },

            PaletteDark = new PaletteDark()
            {
                Primary = "#262626",
                PrimaryLighten = "#4C4C4C",
                Secondary = "#8C8C8C",
                Tertiary = "#666666",
                Surface = "#0D0D0D",
                Background = "#141414",
                AppbarBackground = "#080808",
                Dark = "#2f2f2fff",
                Divider = "#808080",
                GrayDarker = "#1F1F1F",
                Success = "#66BB6A",
                Warning = "#FFA726",
                Error = "#EF5350",
                Info = "#42A5F5"
            },

            Typography = new Typography()
            {
                Default = new DefaultTypography()
                {
                    FontFamily = new[] { "Tahoma", "Geneva", "Verdana", },
                    FontSize = "0.875rem",
                },
                Button = new ButtonTypography()
                {
                    FontFamily = new[] { "Tahoma", "Geneva", "Verdana", },
                    TextTransform = "none",
                    FontSize = "1em",
                }
            },

            LayoutProperties = new LayoutProperties()
            {
                DefaultBorderRadius = "0.4em",
                AppbarHeight = "2rem",
                DrawerWidthLeft = "3.25em",
            },

            Shadows = new Shadow(),
        };
    }

    public static class Instance
    {
        public static StealthTheme Theme { get; set; } = new StealthTheme();
    }
}
