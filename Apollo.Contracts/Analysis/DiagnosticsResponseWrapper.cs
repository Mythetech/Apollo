namespace Apollo.Contracts.Analysis;

public class DiagnosticsResponseWrapper
{
    public IEnumerable<Diagnostic> Payload { get; set; }
}