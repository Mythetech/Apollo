namespace Apollo.Contracts.Compilation;

public record CompilationReferenceResult(bool Success, byte[]? Assembly, List<string?>? Diagnostics = null, TimeSpan BuildTime = default);