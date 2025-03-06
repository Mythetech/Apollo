using System.Reflection;
using Apollo.Compilation;
using Apollo.Components.Solutions;
using Apollo.Contracts.Compilation;
using Microsoft.CodeAnalysis;

namespace Apollo.Test.Components.Code;

public static class TestCompiler
{
    private static CompilationService _compiler = new();
    
    public static CompilationReferenceResult Compile(SolutionModel solution)
    {

        var result = _compiler.Compile(solution.ToContract(), GetDefaultReferences());
        return result;
    }
    
    private static IEnumerable<MetadataReference> GetDefaultReferences()
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Console").Location)

        };

        return references;
    }
}