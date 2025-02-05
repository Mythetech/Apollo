using System.Diagnostics;
using System.Reflection;
using Apollo.Hosting.Logging;
using Microsoft.AspNetCore.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text.Json;
using Apollo.Contracts.Workers;
using Apollo.Infrastructure.Workers;

namespace Apollo.Hosting;

public class HostingCompilationService
{
    public CompilationResult Compile(string code, IEnumerable<MetadataReference> references)
    {
        var stopwatch = Stopwatch.StartNew();

        var transformedCode = MinimalApiTransformer.WrapMinimalApi(code);

        var syntaxTree = CSharpSyntaxTree.ParseText(transformedCode);

        var compilation = CSharpCompilation.Create(
            "WebApiApp",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.ConsoleApplication)
        );

        using var memoryStream = new MemoryStream();
        var emitResult = compilation.Emit(memoryStream);

        stopwatch.Stop();

        if (!emitResult.Success)
        {
            var diagnostics = emitResult.Diagnostics.Select(d => d.GetMessage()).ToList();
            return new CompilationResult(false, null, diagnostics);
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        var assemblyBytes = memoryStream.ToArray();
        var assembly = Assembly.Load(assemblyBytes);

        return new CompilationResult(true, assembly);
    }
    
    public WebApplication? CurrentApp { get; private set; }

    public void Execute(Assembly assembly, Action<string> logCallback, Action<WorkerMessage> messageCallback, CancellationToken cancellationToken)
    {
        if (assembly == null)
        {
            logCallback?.Invoke("No assembly provided to execute");
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var console = new HostingConsoleService(logCallback);
        console.LogInfo("Starting web application execution...");

        CompilationContext.SetConsole(console);
        CompilationContext.SetMessageSender(messageCallback);

        try
        {
            var entryPoint = assembly.EntryPoint;
            if (entryPoint == null)
            {
                logCallback?.Invoke("No entry point found in the assembly.");
                return;
            }

            var parameters = entryPoint.GetParameters();
            var invokeArgs = parameters.Length == 1 && parameters[0].ParameterType == typeof(string[])
                ? new object?[] { Array.Empty<string>() }
                : null;           
            
            entryPoint.Invoke(null, invokeArgs);
            CurrentApp = WebApplication.Current;
            if (CurrentApp == null)
            {
                console.LogWarning("No current app found - did the user code create one?");
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var realException = ex.InnerException ?? ex;
            logCallback?.Invoke($"Execution error: {realException.GetType().Name}: {realException.Message}");
            logCallback?.Invoke(realException.StackTrace ?? "No stack trace available");
            return;
        }
        finally
        {
            stopwatch.Stop();
        }
    }
}

public record CompilationResult(bool Success, Assembly? Assembly, List<string>? Diagnostics = null); 