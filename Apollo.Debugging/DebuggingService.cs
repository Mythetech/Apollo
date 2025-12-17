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
    private DebugSymbolRewritter GetRewritter(string path, ICollection<Breakpoint> breakpoints)
    {
        var fileBreakpoints = breakpoints
            .Where(b => b.File == path || b.File.EndsWith(Path.GetFileName(path)))
            .ToList();
        
        return new DebugSymbolRewritter(fileBreakpoints);
    }
    
    public async Task DebugAsync(Solution solution, IEnumerable<MetadataReference> references, ICollection<Breakpoint> breakpoints, Action<string>? logAction = null, Action<DebuggerEvent>? debugAction = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        logAction?.Invoke($"Starting debug with {breakpoints.Count} breakpoint(s)");
        foreach (var bp in breakpoints)
        {
            logAction?.Invoke($"  Breakpoint: {bp.File}:{bp.Line}");
        }
        
        // Create a global usings file
        var globalUsings = CSharpSyntaxTree.ParseText(@"
global using Apollo.Debugging;
", path: "GlobalUsings.cs");

        var syntaxTrees = new List<SyntaxTree> { globalUsings };
        
        // Add user code trees
        syntaxTrees.AddRange(solution.Items.Select(item =>
        {
            var tree = CSharpSyntaxTree.ParseText(item.Content, path: item.Path);
            
            var fileBreakpoints = breakpoints
                .Where(b => b.File == item.Path || b.File.EndsWith(Path.GetFileName(item.Path)))
                .ToList();
            
            logAction?.Invoke($"File: {item.Path}, Breakpoints: {fileBreakpoints.Count}");

            var rewriter = GetRewritter(item.Path, breakpoints);
            var newRoot = rewriter.Visit(tree.GetRoot());

            return CSharpSyntaxTree.Create(newRoot as CSharpSyntaxNode,
                path: item.Path,
                encoding: tree.Encoding);
        }));

        var compilation = CSharpCompilation.Create(
            solution.Name,
            syntaxTrees,
            references,
            new CSharpCompilationOptions(
                ConvertProjectToOutputKind(solution.Type), 
                concurrentBuild: true, 
                allowUnsafe: true
            ).WithMetadataImportOptions(MetadataImportOptions.All)
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
        };

        DebugRuntime.Initialize(breakpointHandler);
        
        debugAction?.Invoke(new DebuggerEvent(DebugEventType.Started, null, null));

        try
        {
            var entryPoint = assembly.EntryPoint;
            if (entryPoint == null)
            {
                logAction?.Invoke("No entry point found in the assembly.");
                debugAction?.Invoke(new DebuggerEvent(DebugEventType.Error, null, null));
                return;
            }

            var parameters = entryPoint.GetParameters();
            var invokeArgs = parameters.Length == 1 && parameters[0].ParameterType == typeof(string[])
                ? new object?[] { Array.Empty<string>() }
                : null;

            try
            {
                await ExecuteEntryPointAsync(entryPoint, invokeArgs, logAction, debugAction);
            }
            catch (Exception ex)
            {
                var actualException = ex is AggregateException aggEx 
                    ? aggEx.Flatten().InnerException ?? aggEx 
                    : ex;
                
                logAction?.Invoke($"Outer execution error: {actualException.GetType().Name}: {actualException.Message}");
                debugAction?.Invoke(new DebuggerEvent(DebugEventType.Error, null, null));
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logAction?.Invoke($"Execution error: {ex.Message}");
            debugAction?.Invoke(new DebuggerEvent(DebugEventType.Error, null, null));
            return;
        }
        finally
        {
            stopwatch.Stop();
        }

        logAction?.Invoke($"Execution finished in {stopwatch.ElapsedMilliseconds} ms.");
        debugAction?.Invoke(new DebuggerEvent(DebugEventType.Terminated, null, null));
    }

    private async Task ExecuteEntryPointAsync(MethodInfo entryPoint, object?[]? invokeArgs, Action<string>? logAction, Action<DebuggerEvent>? debugAction)
    {
        try
        {
            var result = entryPoint.Invoke(null, invokeArgs);
            
            if (result is Task task)
            {
                await task;
            }
        }
        catch (Exception ex)
        {
            var actualException = ex is System.Reflection.TargetInvocationException tie 
                ? tie.InnerException ?? ex 
                : ex;
            
            if (actualException is AggregateException aggEx)
            {
                var flattened = aggEx.Flatten();
                actualException = flattened.InnerException ?? flattened;
            }
            
            logAction?.Invoke($"Execution error: {actualException.GetType().Name}: {actualException.Message}");
            if (actualException.StackTrace != null)
            {
                logAction?.Invoke(actualException.StackTrace);
            }
            debugAction?.Invoke(new DebuggerEvent(DebugEventType.Error, null, null));
            throw;
        }
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