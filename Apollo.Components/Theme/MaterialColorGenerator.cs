namespace Apollo.Components.Theme;

public static class MaterialColorGenerator
{
    public static (CustomPalette Light, CustomPalette Dark) GenerateFromSeed(string hexColor)
    {
        var hsl = HexToHsl(hexColor);
        
        return (GenerateLightPalette(hsl), GenerateDarkPalette(hsl));
    }
    
    private static CustomPalette GenerateLightPalette(HslColor seed)
    {
        var primary = HslToHex(seed with { Lightness = 40 });
        var primaryLighten = HslToHex(seed with { Lightness = 55 });
        var secondary = HslToHex(seed with { Saturation = seed.Saturation * 0.3, Lightness = 50 });
        var tertiary = HslToHex(new HslColor((seed.Hue + 60) % 360, seed.Saturation * 0.7, 45));
        
        var surface = "#FFFFFF";
        var background = "#FAFAFA";
        var appbarBackground = HslToHex(seed with { Lightness = 25 });
        var dark = HslToHex(seed with { Saturation = seed.Saturation * 0.1, Lightness = 92 });
        var divider = HslToHex(seed with { Lightness = 45 });
        var grayDarker = "#666666";
        
        return new CustomPalette
        {
            Primary = primary,
            PrimaryLighten = primaryLighten,
            Secondary = secondary,
            Tertiary = tertiary,
            Surface = surface,
            Background = background,
            AppbarBackground = appbarBackground,
            Dark = dark,
            Divider = divider,
            GrayDarker = grayDarker,
            Success = "#4CAF50",
            Warning = "#FF9800",
            Error = "#F44336",
            Info = "#2196F3"
        };
    }
    
    private static CustomPalette GenerateDarkPalette(HslColor seed)
    {
        var primary = HslToHex(seed with { Lightness = 15 });
        var primaryLighten = HslToHex(seed with { Lightness = 30 });
        var secondary = HslToHex(seed with { Lightness = 55 });
        var tertiary = HslToHex(new HslColor((seed.Hue + 60) % 360, seed.Saturation * 0.8, 40));
        
        var surface = HslToHex(seed with { Saturation = seed.Saturation * 0.1, Lightness = 5 });
        var background = HslToHex(seed with { Saturation = seed.Saturation * 0.1, Lightness = 8 });
        var appbarBackground = HslToHex(seed with { Saturation = seed.Saturation * 0.15, Lightness = 3 });
        var dark = HslToHex(seed with { Saturation = seed.Saturation * 0.15, Lightness = 18 });
        var divider = HslToHex(seed with { Lightness = 50 });
        var grayDarker = HslToHex(seed with { Saturation = seed.Saturation * 0.1, Lightness = 12 });
        
        return new CustomPalette
        {
            Primary = primary,
            PrimaryLighten = primaryLighten,
            Secondary = secondary,
            Tertiary = tertiary,
            Surface = surface,
            Background = background,
            AppbarBackground = appbarBackground,
            Dark = dark,
            Divider = divider,
            GrayDarker = grayDarker,
            Success = "#66BB6A",
            Warning = "#FFA726",
            Error = "#EF5350",
            Info = "#42A5F5"
        };
    }
    
    private record HslColor(double Hue, double Saturation, double Lightness);
    
    private static HslColor HexToHsl(string hex)
    {
        hex = hex.TrimStart('#');
        
        var r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255.0;
        var g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255.0;
        var b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255.0;
        
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;
        
        var lightness = (max + min) / 2.0 * 100;
        
        double saturation = 0;
        if (delta != 0)
        {
            saturation = delta / (1 - Math.Abs(2 * lightness / 100 - 1)) * 100;
        }
        
        double hue = 0;
        if (delta != 0)
        {
            if (max == r)
                hue = 60 * (((g - b) / delta) % 6);
            else if (max == g)
                hue = 60 * (((b - r) / delta) + 2);
            else
                hue = 60 * (((r - g) / delta) + 4);
        }
        
        if (hue < 0) hue += 360;
        
        return new HslColor(hue, saturation, lightness);
    }
    
    private static string HslToHex(HslColor hsl)
    {
        var h = hsl.Hue;
        var s = Math.Clamp(hsl.Saturation, 0, 100) / 100.0;
        var l = Math.Clamp(hsl.Lightness, 0, 100) / 100.0;
        
        var c = (1 - Math.Abs(2 * l - 1)) * s;
        var x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        var m = l - c / 2;
        
        double r, g, b;
        
        if (h < 60)
            (r, g, b) = (c, x, 0);
        else if (h < 120)
            (r, g, b) = (x, c, 0);
        else if (h < 180)
            (r, g, b) = (0, c, x);
        else if (h < 240)
            (r, g, b) = (0, x, c);
        else if (h < 300)
            (r, g, b) = (x, 0, c);
        else
            (r, g, b) = (c, 0, x);
        
        var ri = (int)Math.Round((r + m) * 255);
        var gi = (int)Math.Round((g + m) * 255);
        var bi = (int)Math.Round((b + m) * 255);
        
        return $"#{ri:X2}{gi:X2}{bi:X2}";
    }
}

