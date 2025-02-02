namespace Apollo.Components.Terminal.Models;

public record TerminalEntry
{
    public string Content { get; init; } = string.Empty;
    public TerminalEntryType Type { get; init; } = TerminalEntryType.Standard;
    public string? CustomClass { get; init; }
    public bool IsCommand { get; init; }
}

public enum TerminalEntryType
{
    Standard,
    Error,
    Success,
    Info,
    Warning
} 