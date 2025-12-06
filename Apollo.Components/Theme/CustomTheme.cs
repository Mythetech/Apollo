using Apollo.Components.Settings;
using MudBlazor;

namespace Apollo.Components.Theme;

public class CustomTheme : EditorThemeDefinition
{
    private readonly CustomThemeData _data;
    
    public CustomThemeData Data => _data;
    
    public override string Name => _data.Name;
    public override bool HideAppIcon => _data.AppBar?.HideAppIcon ?? false;
    public override string AppIconClass => _data.AppBar?.AppIconClass ?? "";
    
    public bool IsCustom => true;
    
    public CustomTheme(CustomThemeData data)
    {
        _data = data;
        BaseTheme = BuildMudTheme(data);
    }
    
    public override string AppBarStyle
    {
        get
        {
            var isDark = AppState?.IsDarkMode ?? true;
            var customStyle = isDark ? _data.AppBar?.DarkStyle : _data.AppBar?.LightStyle;
            return string.IsNullOrEmpty(customStyle) 
                ? base.AppBarStyle 
                : customStyle;
        }
    }
    
    public CustomTheme WithUpdatedData(CustomThemeData newData)
    {
        return new CustomTheme(newData);
    }
    
    private static MudTheme BuildMudTheme(CustomThemeData data)
    {
        return new MudTheme
        {
            PaletteLight = BuildPaletteLight(data.Light),
            PaletteDark = BuildPaletteDark(data.Dark),
            Typography = BuildTypography(data.Typography),
            LayoutProperties = BuildLayoutProperties(data.Layout)
        };
    }
    
    private static PaletteLight BuildPaletteLight(CustomPalette palette)
    {
        var p = new PaletteLight
        {
            Primary = palette.Primary,
            PrimaryLighten = palette.PrimaryLighten,
            Secondary = palette.Secondary,
            Tertiary = palette.Tertiary,
            Surface = palette.Surface,
            Background = palette.Background,
            AppbarBackground = palette.AppbarBackground,
            Dark = palette.Dark,
            Divider = palette.Divider,
            GrayDarker = palette.GrayDarker
        };
        
        if (!string.IsNullOrEmpty(palette.Success)) p.Success = palette.Success;
        if (!string.IsNullOrEmpty(palette.Warning)) p.Warning = palette.Warning;
        if (!string.IsNullOrEmpty(palette.Error)) p.Error = palette.Error;
        if (!string.IsNullOrEmpty(palette.Info)) p.Info = palette.Info;
        
        return p;
    }
    
    private static PaletteDark BuildPaletteDark(CustomPalette palette)
    {
        var p = new PaletteDark
        {
            Primary = palette.Primary,
            PrimaryLighten = palette.PrimaryLighten,
            Secondary = palette.Secondary,
            Tertiary = palette.Tertiary,
            Surface = palette.Surface,
            Background = palette.Background,
            AppbarBackground = palette.AppbarBackground,
            Dark = palette.Dark,
            Divider = palette.Divider,
            GrayDarker = palette.GrayDarker
        };
        
        if (!string.IsNullOrEmpty(palette.Success)) p.Success = palette.Success;
        if (!string.IsNullOrEmpty(palette.Warning)) p.Warning = palette.Warning;
        if (!string.IsNullOrEmpty(palette.Error)) p.Error = palette.Error;
        if (!string.IsNullOrEmpty(palette.Info)) p.Info = palette.Info;
        
        return p;
    }
    
    private static Typography BuildTypography(CustomTypography? typography)
    {
        var t = typography ?? CustomTypography.Default;
        return new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = t.FontFamily,
                TextTransform = "none",
                FontSize = t.FontSize
            },
            Button = new ButtonTypography
            {
                FontFamily = t.FontFamily,
                TextTransform = "none",
                FontSize = t.ButtonFontSize ?? "1em"
            }
        };
    }
    
    private static LayoutProperties BuildLayoutProperties(CustomLayout? layout)
    {
        var l = layout ?? CustomLayout.Default;
        return new LayoutProperties
        {
            DefaultBorderRadius = l.BorderRadius,
            AppbarHeight = l.AppbarHeight,
            DrawerWidthLeft = l.DrawerWidthLeft
        };
    }
}



