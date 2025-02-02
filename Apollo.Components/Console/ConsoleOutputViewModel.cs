namespace Apollo.Components.Console;

public class ConsoleOutputViewModel
{
    /// <summary>
    /// Time the output was received
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }
    
    /// <summary>
    /// Severity of the output
    /// </summary>
    public ConsoleSeverity Severity { get; set; }
    
    /// <summary>
    /// Console output
    /// </summary>
    public string Message { get; set; } = string.Empty;

    public string HtmlId => GetHtmlId();
    
    public string GetHtmlId()
    {
        var safeMessage = new string(Message
            .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
            .ToArray());

        return $"console-{Timestamp.ToUnixTimeMilliseconds()}-{Severity}-{safeMessage}".ToLower();
    }
}