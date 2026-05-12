namespace Apollo.Contracts.Analysis;

public record QuickInfoResult
{
    public string Markdown { get; init; } = "";

    public static QuickInfoResult Empty => new() { Markdown = "" };
}
