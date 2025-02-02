namespace Apollo.Contracts.Analysis;

public record Diagnostic()
{
    public string FilePath { get; init; }
    //public LinePosition Start { get; init; }
    public int StartPosition { get; init; }
    public int StartColumn { get; init; }
    //public LinePosition End { get; init; }
    public int EndPosition { get; init; }
    public int EndColumn { get; init; }
    public string Message { get; init; }
    public int Severity { get; init; }
}