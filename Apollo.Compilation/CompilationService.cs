using System.Diagnostics;
using Apollo.Contracts.Compilation;
using Apollo.Contracts.Solutions;

namespace Apollo.Compilation;

using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Solution = Apollo.Contracts.Solutions.Solution;

public class CompilationService
{
    public CompilationReferenceResult Compile(Solution solution, IEnumerable<MetadataReference> references)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var outputKind = solution.Type == ProjectType.Console ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary;
        
        var syntaxTrees = solution.Items.Select(item =>
            CSharpSyntaxTree.ParseText(item.Content, path: item.Path)).ToList();

        var compilation = CSharpCompilation.Create(
            solution.Name,
            syntaxTrees,
            references,
            new CSharpCompilationOptions(outputKind, concurrentBuild: true, allowUnsafe: true )
        );

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
                string message = "No entry point found in the assembly.";
                logAction?.Invoke(message);
                return new ExecutionResult
                {
                    Error = true,
                    Messages = [message],
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
            var actualException = ex is System.Reflection.TargetInvocationException tie ? tie.InnerException ?? ex : ex;
            logAction?.Invoke($"Execution error: {actualException.Message}");
            if (actualException.StackTrace != null)
            {
                logAction?.Invoke(actualException.StackTrace);
            }
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