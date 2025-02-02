using MudBlazor;

namespace Apollo.Components.Theme;

public class StealthTheme : EditorThemeDefinition
{
    public override string Name => "Stealth";
    
    public override bool HideAppIcon => true;

    public override string AppBarStyle => AppState?.IsDarkMode ?? true
        ? base.AppBarStyle
        : base.AppBarStyle; //"background:linear-gradient(90deg, black 0%, whitesmoke 12%);";

    public StealthTheme()
    {
        BaseTheme = new()
        {
            PaletteLight = new PaletteLight()
            {
                Primary = "#ecf0f1",
                Secondary = "#2c3e50",
                Success = "#10b981",
                Tertiary = "#999",
                Background = "#FAFAFA",
                Surface = "#FFFFFF",
                AppbarBackground = "#697677", //"#7b8a8b",
                Dark = "#DDD",
                GrayDarker = "#666"
            },

            PaletteDark = new PaletteDark()
            {
                Secondary = "#2c3e50",
                Primary = "#1e2121",
                Tertiary = "#666",
                Success = "#10b981",
                Background = "#121212",
                Surface = "#1E1E1E",
                AppbarBackground = "#1E1E1E",
                GrayDarker = "#111"
            },

            Typography = new Typography()
            {
                Default = new DefaultTypography()
                {
                    FontFamily = new[] { "Tahoma", "Geneva", "Verdana", },
                    FontSize = "0.875rem",
                    FontWeight = "400",
                    LineHeight = "1.43",
                    LetterSpacing = ".01071em",
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