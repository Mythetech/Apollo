using Apollo.Contracts.Solutions;

namespace Apollo.Contracts.Analysis;

public class DiagnosticRequestWrapper
{
    public string Uri { get; set; } = string.Empty;
    public Solution Solution { get; set; } = new();
}