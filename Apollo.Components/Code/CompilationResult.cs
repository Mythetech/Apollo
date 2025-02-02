using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Apollo.Components.Code;

public class CompilationResult
{
    public bool Success { get; }
    
    public Assembly? CompiledAssembly { get; }

    public CompilationResult(bool success, Assembly? compiledAssembly)
    {
        Success = success;
        CompiledAssembly = compiledAssembly;
    }
}