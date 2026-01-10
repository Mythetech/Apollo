using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using Apollo.Compilation;
using Apollo.Compilation.Worker;
using Apollo.Contracts.Solutions;
using Apollo.Contracts.Workers;
using Apollo.Infrastructure;
using Apollo.Infrastructure.Workers;
using KristofferStrube.Blazor.WebWorkers;
using Microsoft.CodeAnalysis;
using Solution = Apollo.Contracts.Solutions.Solution;

if (!OperatingSystem.IsBrowser())
    throw new PlatformNotSupportedException("Can only be run in the browser!");

LogMessageWriter.Log("Starting up worker...", LogSeverity.Debug);

bool keepRunning = true;

var resolver = new MetadataReferenceResourceProvider(HostAddress.BaseUri);

byte[]? asmCache = [];
List<NuGetReference> nugetAssemblyCache = [];
Dictionary<string, Assembly> loadedNuGetAssemblies = new(StringComparer.OrdinalIgnoreCase);

ConcurrentBag<string> executionMessages = [];

AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
{
    var assemblyName = new AssemblyName(args.Name);
    LogMessageWriter.Log($"Assembly resolve requested: {assemblyName.Name}", LogSeverity.Debug);
    
    if (loadedNuGetAssemblies.TryGetValue(assemblyName.Name ?? "", out var assembly))
    {
        LogMessageWriter.Log($"Resolved from NuGet cache: {assemblyName.Name}", LogSeverity.Debug);
        return assembly;
    }
    
    return null;
};

Console.SetOut(new WorkerConsoleWriter());
Console.SetError(new WorkerConsoleWriter());

Imports.RegisterOnMessage(async e =>
{
    var payload = e.GetPropertyAsString("data");
    
    var message = JsonSerializer.Deserialize<WorkerMessage>(payload);
    try
    {
        switch (message.Action.ToLower())
        {
            case "compile":
                var solution = JsonSerializer.Deserialize<Solution>(message.Payload);
                var references = new List<MetadataReference>
                {
                    await resolver.GetMetadataReferenceAsync("System.Private.CoreLib.wasm"),
                    await resolver.GetMetadataReferenceAsync("System.Runtime.wasm"),
                    await resolver.GetMetadataReferenceAsync("System.Console.wasm"),
                    await resolver.GetMetadataReferenceAsync("xunit.assert.wasm"),
                    await resolver.GetMetadataReferenceAsync("xunit.core.wasm")
                };

                var isRazorProject = solution.Type == ProjectType.RazorClassLibrary ||
                                     solution.Items.Any(i => i.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase));

                if (isRazorProject)
                {
                    try
                    {
                        references.Add(await resolver.GetMetadataReferenceAsync("System.Threading.Tasks.wasm"));
                        references.Add(await resolver.GetMetadataReferenceAsync("System.Collections.wasm"));
                        references.Add(await resolver.GetMetadataReferenceAsync("System.Linq.wasm"));
                        references.Add(await resolver.GetMetadataReferenceAsync("System.ObjectModel.wasm"));
                        references.Add(await resolver.GetMetadataReferenceAsync("System.ComponentModel.wasm"));
                        references.Add(await resolver.GetMetadataReferenceAsync("System.ComponentModel.Primitives.wasm"));
                        LogMessageWriter.Log("Added system references for Razor project", LogSeverity.Debug);
                    }
                    catch (Exception ex)
                    {
                        LogMessageWriter.Log($"Warning: Could not load system references: {ex.Message}", LogSeverity.Warning);
                    }

                    try
                    {
                        references.Add(await resolver.GetMetadataReferenceAsync("Microsoft.AspNetCore.Components.wasm"));
                        references.Add(await resolver.GetMetadataReferenceAsync("Microsoft.AspNetCore.Components.Web.wasm"));
                        LogMessageWriter.Log("Added Blazor component references for Razor project", LogSeverity.Debug);
                    }
                    catch (Exception ex)
                    {
                        LogMessageWriter.Log($"Warning: Could not load Blazor references: {ex.Message}", LogSeverity.Warning);
                    }
                }

                nugetAssemblyCache = solution.NuGetReferences ?? [];

                foreach (var nugetRef in nugetAssemblyCache)
                {
                    if (nugetRef.AssemblyData?.Length > 0)
                    {
                        var nugetReference = MetadataReference.CreateFromImage(nugetRef.AssemblyData);
                        references.Add(nugetReference);
                        LogMessageWriter.Log($"Added NuGet reference: {nugetRef.AssemblyName}", LogSeverity.Debug);
                    }
                }

                var result = isRazorProject
                    ? new RazorCompilationService().Compile(solution, references)
                    : new CompilationService().Compile(solution, references);

                asmCache = result.Assembly;

                var msg = new WorkerMessage()
                {
                    Action = "compile_completed",
                    Payload = JsonSerializer.Serialize(result)
                };

                Imports.PostMessage(JsonSerializer.Serialize(msg));
                break;

            case "execute":
                // Load NuGet assemblies into runtime first and register for resolution
                foreach (var nugetRef in nugetAssemblyCache)
                {
                    if (nugetRef.AssemblyData?.Length > 0)
                    {
                        try
                        {
                            var loadedAsm = Assembly.Load(nugetRef.AssemblyData);
                            var asmName = loadedAsm.GetName().Name;
                            if (!string.IsNullOrEmpty(asmName))
                            {
                                loadedNuGetAssemblies[asmName] = loadedAsm;
                            }
                            LogMessageWriter.Log($"Loaded NuGet assembly for runtime: {nugetRef.AssemblyName} ({asmName})", LogSeverity.Debug);
                        }
                        catch (Exception ex)
                        {
                            LogMessageWriter.Log($"Failed to load NuGet assembly {nugetRef.AssemblyName}: {ex.Message}", LogSeverity.Warning);
                        }
                    }
                }
                
                // Load user assembly and run
                byte[]? asm = Convert.FromBase64String(message.Payload);

                var assembly = Assembly.Load(asm.Length > 0 ? asm : asmCache);
                var service = new CompilationService();
                var executionResult = service.Execute(assembly, LogCallback, CancellationToken.None);
                executionResult.Messages = executionMessages.ToList();
                var executedMsg = new WorkerMessage()
                {
                    Action = "execute_completed",
                    Payload = JsonSerializer.Serialize(executionResult)
                };

                Imports.PostMessage(JsonSerializer.Serialize(executedMsg));
                executionMessages.Clear();
                break;
        }
    }
    catch (Exception ex)
    {
        var errMessage = new WorkerMessage()
        {
            Action = "error",
            Payload = ex.Message
        };
        
        Imports.PostMessage(JsonSerializer.Serialize(errMessage));
    }
});

LogMessageWriter.Log("Worker ready", LogSeverity.Debug);

while (keepRunning)
    await Task.Delay(100);

return;

void LogCallback(string message)
{
    executionMessages.Add(message);
}