using MudBlazor;

namespace Apollo.Components.Theme;

public class ApolloTheme : EditorThemeDefinition
{
    public override string Name => "Apollo";
    public override string AppBarStyle => AppState?.IsDarkMode ?? true 
        ? "background: linear-gradient(90deg, var(--mud-palette-surface), var(--mud-palette-tertiary) 50%, var(--mud-palette-surface) 100%);" 
        : "background: linear-gradient(90deg, rgba(0,0,0,0.9) 0%, var(--mud-palette-secondary-lighten) 15%, var(--mud-palette-secondary) 40%, var(--mud-palette-secondary-darken) 60%, rgba(255, 255, 255, 0.95));";
    public ApolloTheme()
    {
        BaseTheme = new()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = "#FFFFFF",
                PrimaryLighten = "#3B3B3B",
                AppbarBackground = "#000",
                Secondary = "#FFB606",
                Tertiary = "#B28228",
                Surface = "#FFFFFF",
                Dark = "#F3F3E6",
                OverlayDark = "#FFFFFF4a",
                Divider = "#FFB606",
                GrayDarker = "#666"
            },

            PaletteDark = new PaletteDark()
            {
                Primary = "#1C1C1C",
                PrimaryLighten = "#3B3B3B",
                AppbarBackground = "#000",
                Secondary = "#FFB606",
                Tertiary = "#B28228",
                Surface = "#000",
                Dark = "#333",
                Divider = "#FFB606",
                GrayDarker = "#222"
            },

            Typography = new Typography()
            {
                Default = new DefaultTypography()
                {
                    FontFamily = new[] { "Tahoma", "Geneva", "Verdana", },
                    TextTransform = "none",
                    FontSize = "0.75",
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
                DefaultBorderRadius = "0.5em",
                AppbarHeight = "3rem",
                DrawerWidthLeft = "3.25em",
            },
        };
    }
    public static class Instance
    {
        public static ApolloTheme Theme { get; set; } = new ApolloTheme();
    }

}