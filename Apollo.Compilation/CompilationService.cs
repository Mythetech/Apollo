using System.Diagnostics;
using Apollo.Contracts.Compilation;

namespace Apollo.Compilation;

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Solution = Apollo.Contracts.Solutions.Solution;

public class CompilationService
{
    private const int MaxExecutionTimeInSeconds = 5;
    
    private OutputKind _outputKind = OutputKind.ConsoleApplication;

    public CompilationService()
    {
        
    }

    public CompilationService(OutputKind outputKind)
    {
        _outputKind = outputKind;
    }

    public CompilationReferenceResult Compile(Solution solution, IEnumerable<MetadataReference> references)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Parse syntax trees from solution items
        var syntaxTrees = solution.Items.Select(item =>
            CSharpSyntaxTree.ParseText(item.Content, path: item.Path)).ToList();

        var compilation = CSharpCompilation.Create(
            solution.Name,
            syntaxTrees,
            references,
            new CSharpCompilationOptions(_outputKind, concurrentBuild: true, allowUnsafe: true )
        );

        // Emit compiled assembly
        using var memoryStream = new MemoryStream();
        var emitResult = compilation.Emit(memoryStream);

        stopwatch.Stop();

        if (!emitResult.Success)
        {
            var diagnostics = emitResult.Diagnostics.Select(d => d.GetMessage()).ToList();
            return new CompilationReferenceResult(false, null, diagnostics, stopwatch.Elapsed);
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        var assemblyBytes = memoryStream.ToArray();

        return new CompilationReferenceResult(true, assemblyBytes, null, stopwatch.Elapsed);
    }


    public ExecutionResult Execute(Assembly assembly, Action<string> logAction, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var entryPoint = assembly.EntryPoint;
            if (entryPoint == null)
            {
                logAction?.Invoke("No entry point found in the assembly.");
                return new ExecutionResult
                {
                    Error = true
                };
            }

            var parameters = entryPoint.GetParameters();
            var invokeArgs = parameters.Length == 1 && parameters[0].ParameterType == typeof(string[])
                ? new object?[] { Array.Empty<string>() }
                : null;           
       
                entryPoint.Invoke(null, invokeArgs);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logAction?.Invoke($"Execution error: {ex.Message}");
            return new ExecutionResult
            {
                Error = true,
                ExecutionTime = stopwatch.Elapsed
            };
        }
        finally
        {
            stopwatch.Stop();
        }

        return new ExecutionResult
        {
            ExecutionTime = stopwatch.Elapsed
        };
    }
}