using System.Diagnostics;
using System.Reflection;
using Apollo.Contracts.Compilation;
using Apollo.Contracts.Debugging;
using Apollo.Contracts.Solutions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Apollo.Debugging;

public class DebuggingService
{
    private DebugSymbolRewritter GetRewritter(string path)
    {
        if (path.Contains("Program"))
        {
            return new DebugSymbolRewritter([new Breakpoint(path, 11)]);
        }

        return new DebugSymbolRewritter([]);
    }
    
    public async Task DebugAsync(Solution solution, IEnumerable<MetadataReference> references, Action<string>? logAction = null, Action<DebuggerEvent>? debugAction = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Create a global usings file
        var globalUsings = CSharpSyntaxTree.ParseText(@"
global using Apollo.Debugging;
", path: "GlobalUsings.cs");

        var syntaxTrees = new List<SyntaxTree> { globalUsings };
        
        // Add user code trees
        syntaxTrees.AddRange(solution.Items.Select(item =>
        {
            var tree = CSharpSyntaxTree.ParseText(item.Content, path: item.Path);
            logAction?.Invoke(tree.ToString());

            var rewriter = GetRewritter(item.Path);
            var newRoot = rewriter.Visit(tree.GetRoot());
            
            logAction?.Invoke("Transformed code:");
            logAction?.Invoke(newRoot.ToFullString());

            return CSharpSyntaxTree.Create(newRoot as CSharpSyntaxNode,
                path: item.Path,
                encoding: tree.Encoding);
        }));

        var compilation = CSharpCompilation.Create(
            solution.Name,
            syntaxTrees,
            references,  // Use our updated references
            new CSharpCompilationOptions(
                ConvertProjectToOutputKind(solution.Type), 
                concurrentBuild: true, 
                allowUnsafe: true
            )
        );

        using var memoryStream = new MemoryStream();
        var emitResult = compilation.Emit(memoryStream);

        stopwatch.Stop();
        
        logAction?.Invoke($"Compilation {(emitResult.Success ? "succeeded" : "failed")} in {stopwatch.ElapsedMilliseconds}ms");

        if (!emitResult.Success)
        {
            var diagnostics = emitResult.Diagnostics.Select(d => d.GetMessage()).ToList();
            foreach (string diagnostic in diagnostics)
            {
                logAction?.Invoke(diagnostic);
            }
            return;
        }
        

        memoryStream.Seek(0, SeekOrigin.Begin);
        var assemblyBytes = memoryStream.ToArray();
        
        var assembly = Assembly.Load(assemblyBytes);
        
        stopwatch = Stopwatch.StartNew();

        // Initialize debug runtime before executing
        var breakpointHandler = (string file, int line) =>
        {
            logAction?.Invoke($"Hit breakpoint at {file}:{line}");
            debugAction?.Invoke(new DebuggerEvent(DebugEventType.Paused, 
                new DebugLocation(file, line, line), default));
            DebugRuntime.Pause();
        };

        DebugRuntime.Initialize(breakpointHandler);

        try
        {
            var entryPoint = assembly.EntryPoint;
            if (entryPoint == null)
            {
                logAction?.Invoke("No entry point found in the assembly.");
                return;
            }

            var parameters = entryPoint.GetParameters();
            var invokeArgs = parameters.Length == 1 && parameters[0].ParameterType == typeof(string[])
                ? new object?[] { Array.Empty<string>() }
                : null;

            var thread = new Thread(() => entryPoint.Invoke(null, invokeArgs));

            DebugRuntime.ManagedCurrentThread = thread;
            
            thread.Start();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logAction?.Invoke($"Execution error: {ex.Message}");
            return;
        }
        finally
        {
            stopwatch.Stop();
        }

        logAction?.Invoke($"Execution finished in {stopwatch.ElapsedMilliseconds} ms.");
    }

    private OutputKind ConvertProjectToOutputKind(ProjectType projectType)
    {
        return projectType switch
        {
            ProjectType.Console => OutputKind.ConsoleApplication,
            ProjectType.WebApi => OutputKind.DynamicallyLinkedLibrary,
            _ => OutputKind.ConsoleApplication
        };
    }
}