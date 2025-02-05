using Apollo.Contracts.Debugging;
using Microsoft.CodeAnalysis;

namespace Apollo.Debugging;

public class DebugTransformer 
{
    public static SyntaxTree InjectDebugSymbols(SyntaxTree sourceTree, ICollection<Breakpoint> breakpoints)
    {
        var rewriter = new DebugSymbolRewritter(breakpoints);
        return sourceTree.WithRootAndOptions(
            rewriter.Visit(sourceTree.GetRoot()),
            sourceTree.Options
        );
    }
    
    public static string InjectDebugRuntime(string assemblySource)
    {
        // Add debug runtime support code
        return $@"
            {assemblySource}
            
            public static class DebugRuntime 
            {{
                public static Action<string, int>? OnBreakpoint;
                public static void CheckBreakpoint(string file, int line) 
                    => OnBreakpoint?.Invoke(file, line);
            }}";
    }
}