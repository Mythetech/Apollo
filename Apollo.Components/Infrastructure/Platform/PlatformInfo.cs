namespace Apollo.Components.Infrastructure.Platform;

public static class PlatformInfo
{
    public static bool IsMac(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent)) return false;
        return userAgent.Contains("Mac", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetMetaKeySymbol(string? userAgent)
    {
        return IsMac(userAgent) ? "⌘" : "\u229e";
    }
}