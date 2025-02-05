using System.Diagnostics;
using System.Reflection;
using Apollo.Contracts.Compilation;
using Apollo.Contracts.Solutions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Apollo.Debugging;

public class DebuggingService
{
    public async Task DebugAsync(Solution solution, IEnumerable<MetadataReference> references, Action<string>? logAction = null)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var syntaxTrees = solution.Items.Select(item =>
            CSharpSyntaxTree.ParseText(item.Content, path: item.Path)).ToList();

        var compilation = CSharpCompilation.Create(
            solution.Name,
            syntaxTrees,
            references,
            new CSharpCompilationOptions(ConvertProjectToOutputKind(solution.Type), concurrentBuild: true, allowUnsafe: true )
        );

        using var memoryStream = new MemoryStream();
        var emitResult = compilation.Emit(memoryStream);

        stopwatch.Stop();

        if (!emitResult.Success)
        {
            var diagnostics = emitResult.Diagnostics.Select(d => d.GetMessage()).ToList();
            foreach (string diagnostic in diagnostics)
            {
                logAction?.Invoke(diagnostic);
            }
        }
        
        logAction?.Invoke($"Compilation succeeded in {stopwatch.ElapsedMilliseconds}ms");

        memoryStream.Seek(0, SeekOrigin.Begin);
        var assemblyBytes = memoryStream.ToArray();
        
        var assembly = Assembly.Load(assemblyBytes);
        
        stopwatch = Stopwatch.StartNew();

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
       
                entryPoint.Invoke(null, invokeArgs);
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