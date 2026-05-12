using System.Collections.Generic;

namespace Apollo.Contracts.Analysis;

public record SignatureHelpResult
{
    public List<SignatureInfo> Signatures { get; init; } = [];
    public int ActiveSignature { get; init; }
    public int ActiveParameter { get; init; }
}

public record SignatureInfo
{
    public string Label { get; init; } = "";
    public string? Documentation { get; init; }
    public string? Name { get; init; }
    public IEnumerable<ParameterInfo>? Parameters { get; init; }
}

public record ParameterInfo
{
    public string Label { get; init; } = "";
    public string? Name { get; init; }
    public string? Documentation { get; init; }
}
