using MudBlazor;

namespace Apollo.Components.Theme
{
    /// <summary>
    /// Siren M3 theme - modern Material 3 inspired design
    /// </summary>
    public class SirenTheme : EditorThemeDefinition
    {
        public override string Name => "Siren";

        public override bool HideAppIcon => true;

        public override string AppIconClass => "siren-app-icon";

        public override string AppBarStyle => (AppState?.IsDarkMode ?? true) ?
            "background: linear-gradient(190deg, var(--mud-palette-primary) 70%, var(--mud-palette-primary-lighten));" :
            "background: linear-gradient(190deg, var(--mud-palette-primary) 70%, var(--mud-palette-primary-lighten));";

        public SirenTheme()
        {
            BaseTheme = new() {
            PaletteLight = new PaletteLight()
            {
                Primary = "#0085CC",
                PrimaryLighten = "#1AB0FF",
                Secondary = "#598BA6",
                Tertiary = "#5A22C3",
                Surface = "#FFFFFF",
                Background = "#FAFAFA",
                AppbarBackground = "#005380",
                Dark = "#E9EBED",
                Divider = "#0096E6",
                GrayDarker = "#666666",
                Success = "#4CAF50",
                Warning = "#FF9800",
                Error = "#F44336",
                Info = "#2196F3"
            },
            PaletteDark = new PaletteDark()
            {
                Primary = "#00324D",
                PrimaryLighten = "#006499",
                Secondary = "#1AB0FF",
                Tertiary = "#4D14B8",
                Surface = "#0B0D0E",
                Background = "#121516",
                AppbarBackground = "#050505ff",
                Dark = "#273035",
                Divider = "#00A7FF",
                GrayDarker = "#1C2022",
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
                    TextTransform = "none",
                    FontSize = "2",
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
                DefaultBorderRadius = "1.25em",
                AppbarHeight = "2rem",
                DrawerWidthLeft = "3.25em",
            },

            };
        }
        public static class Instance
        {
            public static SirenTheme Theme { get; set; } = new();
        }
    }
}
