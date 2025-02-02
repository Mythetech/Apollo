using System.Reflection;

namespace Apollo.Contracts.Compilation;

public record CompilationResult(bool Success, Assembly? Assembly, List<string>? Diagnostics = null);