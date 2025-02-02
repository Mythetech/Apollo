using MudBlazor;

namespace Apollo.Components.Theme
{
    /// <summary>
    /// Copied from Siren
    /// </summary>
    public class SirenTheme : EditorThemeDefinition
    {
        public override string Name => "Siren";

        public override bool HideAppIcon => false;

        public override string AppIconClass => "siren-app-icon";
        
        public override string AppBarStyle => (AppState?.IsDarkMode ?? true) ? 
            "box-shadow:1px 1px 4px 0px var(--mud-palette-primary); background:linear-gradient(90deg,var(--mud-palette-appbar-background) 30%, rgba(0,0,0,0.25) 100%);" :
            "box-shadow:1px 1px 8px 1px var(--mud-palette-primary);background:linear-gradient(90deg, var(--mud-palette-primary) 20%, var(--mud-palette-tertiary) 70%, rgba(255,255,255,0.25) 100%);";
        
        public SirenTheme()
        {
            BaseTheme = new() {
            PaletteLight = new PaletteLight()
            {
                Primary = SirenColors.Primary,
                Secondary = SirenColors.Darken,
                Tertiary = SirenColors.Tertiary,
                HoverOpacity = 0.5,
                Background = SirenColors.Light,
                AppbarBackground = "#8fd8ff",
                Black = SirenColors.Black,
                Surface = "#FFFFFF",
                ActionDefault = SirenColors.Primary,
                ActionDisabled = SirenColors.ActionDisabled,
                TableHover = SirenColors.Tertiary,
                Dark = "#016a94",
                Divider = SirenColors.Primary,
                GrayDarker = "#666666"
            },
            PaletteDark = new PaletteDark()
            {
                Primary = SirenColors.Primary,
                Secondary = SirenColors.Light,
                Tertiary = SirenColors.Primary,
                HoverOpacity = 0.5,
                TableHover = SirenColors.Dark.AppBarBackground,
                AppbarBackground = SirenColors.Dark.AppBarBackground,
                Background = SirenColors.Black,
                Black = SirenColors.Black,
                Surface = SirenColors.Black,
                ActionDefault = SirenColors.Primary,
                Dark = SirenColors.Darken,
                Divider = SirenColors.Primary,
                GrayDarker = "#222",
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
            public static SirenTheme Theme { get; set; } = new();
        }
    }
    
    internal static class SirenColors
    {
        public static class Dark
        {
            //public static string Primary => "03045e";
            public static string Primary => "00001A";

            public static string Secondary => "640ADB";

            public static string Tertiary => "002538";

            public static string AppBarBackground => "00162E";

            //public static string AppBarBackground => "001520";

        }

        public static string Primary => "0077B6";

        public static string Secondary => "170035";

        public static string Tertiary => "AED2E5";

        public static string Light => "caf0f8";

        public static string Darken => "003449";

        public static string Black => "000000";

        public static string Action => "";

        public static string ActionDisabled => "AED2E5";

        public static string Hash(string s)
        {
            return $"#{s}";
        }
    }
    
    
}

