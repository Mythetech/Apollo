using System.Text.Json;
using System.Text.Json.Serialization;

namespace Apollo.Components.Theme;

public record CustomThemeData
{
    public const int CurrentVersion = 1;
    
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public int Version { get; init; } = CurrentVersion;
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ModifiedAt { get; init; } = DateTimeOffset.UtcNow;
    
    public required CustomPalette Light { get; init; }
    public required CustomPalette Dark { get; init; }
    public CustomTypography? Typography { get; init; }
    public CustomLayout? Layout { get; init; }
    public CustomAppBar? AppBar { get; init; }
    
    public static CustomThemeData CreateDefault(string name) => new()
    {
        Name = name,
        Light = CustomPalette.DefaultLight,
        Dark = CustomPalette.DefaultDark,
        Typography = CustomTypography.Default,
        Layout = CustomLayout.Default,
        AppBar = CustomAppBar.Default
    };
    
    public static CustomThemeData FromBuiltIn(EditorThemeDefinition theme) => new()
    {
        Name = $"{theme.Name} (Copy)",
        Description = $"Based on {theme.Name}",
        Light = CustomPalette.FromMudPalette(theme.BaseTheme.PaletteLight),
        Dark = CustomPalette.FromMudPalette(theme.BaseTheme.PaletteDark),
        Typography = CustomTypography.FromMudTypography(theme.BaseTheme.Typography),
        Layout = CustomLayout.FromMudLayout(theme.BaseTheme.LayoutProperties),
        AppBar = new CustomAppBar
        {
            HideAppIcon = theme.HideAppIcon,
            AppIconClass = theme.AppIconClass
        }
    };
}

public record CustomPalette
{
    public string Primary { get; init; } = "#1C1C1C";
    public string PrimaryLighten { get; init; } = "#3B3B3B";
    public string Secondary { get; init; } = "#FFB606";
    public string Tertiary { get; init; } = "#B28228";
    public string Surface { get; init; } = "#000000";
    public string Background { get; init; } = "#121212";
    public string AppbarBackground { get; init; } = "#000000";
    public string Dark { get; init; } = "#333333";
    public string Divider { get; init; } = "#FFB606";
    public string GrayDarker { get; init; } = "#222222";
    
    public string? Success { get; init; }
    public string? Warning { get; init; }
    public string? Error { get; init; }
    public string? Info { get; init; }
    
    public static CustomPalette DefaultLight => new()
    {
        Primary = "#FFFFFF",
        PrimaryLighten = "#3B3B3B",
        Secondary = "#FFB606",
        Tertiary = "#B28228",
        Surface = "#FFFFFF",
        Background = "#FAFAFA",
        AppbarBackground = "#000000",
        Dark = "#F3F3E6",
        Divider = "#FFB606",
        GrayDarker = "#666666"
    };
    
    public static CustomPalette DefaultDark => new()
    {
        Primary = "#1C1C1C",
        PrimaryLighten = "#3B3B3B",
        Secondary = "#FFB606",
        Tertiary = "#B28228",
        Surface = "#000000",
        Background = "#121212",
        AppbarBackground = "#000000",
        Dark = "#333333",
        Divider = "#FFB606",
        GrayDarker = "#222222"
    };
    
    public static CustomPalette FromMudPalette(MudBlazor.Palette palette) => new()
    {
        Primary = palette.Primary.Value,
        PrimaryLighten = palette.PrimaryLighten,
        Secondary = palette.Secondary.Value,
        Tertiary = palette.Tertiary.Value,
        Surface = palette.Surface.Value,
        Background = palette.Background.Value,
        AppbarBackground = palette.AppbarBackground.Value,
        Dark = palette.Dark.Value,
        Divider = palette.Divider.Value,
        GrayDarker = palette.GrayDarker,
        Success = palette.Success.Value,
        Warning = palette.Warning.Value,
        Error = palette.Error.Value,
        Info = palette.Info.Value
    };
}

public record CustomTypography
{
    public string[] FontFamily { get; init; } = ["Tahoma", "Geneva", "Verdana"];
    public string FontSize { get; init; } = "0.75";
    public string? ButtonFontSize { get; init; } = "1em";
    
    public static CustomTypography Default => new();
    
    public static CustomTypography FromMudTypography(MudBlazor.Typography typography) => new()
    {
        FontFamily = typography.Default.FontFamily ?? ["Tahoma", "Geneva", "Verdana"],
        FontSize = typography.Default.FontSize ?? "0.75",
        ButtonFontSize = typography.Button.FontSize ?? "1em"
    };
}

public record CustomLayout
{
    public string BorderRadius { get; init; } = "0.5em";
    public string AppbarHeight { get; init; } = "3rem";
    public string DrawerWidthLeft { get; init; } = "3.25em";
    
    public static CustomLayout Default => new();
    
    public static CustomLayout FromMudLayout(MudBlazor.LayoutProperties layout) => new()
    {
        BorderRadius = layout.DefaultBorderRadius ?? "0.5em",
        AppbarHeight = layout.AppbarHeight ?? "3rem",
        DrawerWidthLeft = layout.DrawerWidthLeft ?? "3.25em"
    };
}

public record CustomAppBar
{
    public bool HideAppIcon { get; init; }
    public string AppIconClass { get; init; } = "";
    public string? LightStyle { get; init; }
    public string? DarkStyle { get; init; }
    
    public static CustomAppBar Default => new();
}

public static class CustomThemeSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    public static string Serialize(CustomThemeData theme) 
        => JsonSerializer.Serialize(theme, Options);
    
    public static string SerializeMany(IEnumerable<CustomThemeData> themes) 
        => JsonSerializer.Serialize(themes, Options);
    
    public static CustomThemeData? Deserialize(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<CustomThemeData>(json, Options);
        }
        catch
        {
            return null;
        }
    }
    
    public static List<CustomThemeData>? DeserializeMany(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<CustomThemeData>>(json, Options);
        }
        catch
        {
            return null;
        }
    }
    
    public static (bool IsValid, string? Error) Validate(string json)
    {
        try
        {
            var theme = JsonSerializer.Deserialize<CustomThemeData>(json, Options);
            if (theme == null) return (false, "Invalid theme format");
            if (string.IsNullOrWhiteSpace(theme.Name)) return (false, "Theme name is required");
            if (theme.Light == null) return (false, "Light palette is required");
            if (theme.Dark == null) return (false, "Dark palette is required");
            return (true, null);
        }
        catch (JsonException ex)
        {
            return (false, $"JSON parsing error: {ex.Message}");
        }
    }
}



