using Apollo.Contracts.Workers;
using Microsoft.Extensions.Logging;

namespace Apollo.Components.Console;

public enum ConsoleSeverity
{
    Debug,
    Trace,
    Info,
    Warning,
    Error,
    Success
}

public static class ConsoleSeverityConverterExtensions
{
    public static ConsoleSeverity ToConsoleSeverity(this LogSeverity severity)
    {
        return severity switch
        {
            LogSeverity.Debug => ConsoleSeverity.Debug,
            LogSeverity.Information => ConsoleSeverity.Info,
            LogSeverity.Warning => ConsoleSeverity.Warning,
            LogSeverity.Error => ConsoleSeverity.Error,
            _ => ConsoleSeverity.Info
        };
    }

    public static ConsoleSeverity ToConsoleSeverity(this string s)
    {
        return s.ToLowerInvariant() switch
        {
            "debug" => ConsoleSeverity.Debug,
            "trace" => ConsoleSeverity.Trace,
            "info" => ConsoleSeverity.Info,
            "information" => ConsoleSeverity.Info,
            "warning" => ConsoleSeverity.Warning,
            "error" => ConsoleSeverity.Error,
            "success" => ConsoleSeverity.Success,
            _ => ConsoleSeverity.Info
        };
    }
}