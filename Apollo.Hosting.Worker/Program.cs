using System;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Contracts.Solutions;
using Apollo.Hosting;
using Apollo.Hosting.Worker;
using Apollo.Infrastructure;
using Apollo.Infrastructure.Workers;
using KristofferStrube.Blazor.WebWorkers;
using Microsoft.AspNetCore.Builder;
using Microsoft.CodeAnalysis;

if (!OperatingSystem.IsBrowser())
    throw new PlatformNotSupportedException("Can only be run in the browser!");

Imports.PostMessage(
    WorkerLogWriter.DebugMessage("Starting up hosting worker...")
);

var resolver = new MetadataReferenceResourceProvider(HostAddress.BaseUri);
var loggerBridge = new HostingLogger();
WebApplication? _currentApp = default;
HostingCompilationService? _service = default;
Dictionary<string, Assembly> loadedNuGetAssemblies = new(StringComparer.OrdinalIgnoreCase);

AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
{
    var assemblyName = new AssemblyName(args.Name);
    loggerBridge.LogDebug($"Assembly resolve requested: {assemblyName.Name}");
    
    if (loadedNuGetAssemblies.TryGetValue(assemblyName.Name ?? "", out var assembly))
    {
        loggerBridge.LogDebug($"Resolved from NuGet cache: {assemblyName.Name}");
        return assembly;
    }
    
    return null;
};

Console.SetOut(new HostingConsoleWriter(ExecutionLogCallback));
Console.SetError(new HostingConsoleWriter(ExecutionLogCallback));

bool keepRunning = true;

Imports.RegisterOnMessage(async e =>
{
    var payload = e.GetPropertyAsString("data");
    
    var message = JsonSerializer.Deserialize<WorkerMessage>(payload);
    
    loggerBridge.LogDebug("Received message: " + message.Action);
    
    try
    {
        switch (message.Action.ToLower())
        {
            case WorkerActions.Run:
                await HandleRunMessage(message);
                break;
            case WorkerActions.Debug:
                break;
            case WorkerActions.Send:
                await HandleRouteMessage(message);
                break;
            case WorkerActions.Stop:
                await WebApplication.CurrentCancellationToken.CancelAsync();
                WebApplication.Current = null;
                _currentApp = null;
                break;
        }
    }
    catch (Exception ex)
    {
        Imports.PostMessage(ErrorWriter.SerializeErrorToWorkerMessage(ex));
    }
});

Imports.PostMessage(
    WorkerLogWriter.DebugMessage("Hosting worker ready")
);

while (keepRunning)
    await Task.Delay(100);

return;

void ExecutionLogCallback(string message)
{
    if (message.Contains("[Trace]"))
    {
        loggerBridge.LogTrace(message.Replace("[Trace]", "").Trim());
    }
    else if (message.Contains("[Error]"))
    {
        loggerBridge.LogError(message.Replace("[Error]", "").Trim());
    }
    else if (message.Contains("[Debug]"))
    {
        loggerBridge.LogDebug(message.Replace("[Debug]", "").Trim());
    }
    else if (message.Contains("[Warning]"))
    {
        loggerBridge.LogWarning(message.Replace("[Warning]", "").Trim());
    }
    else
    {
        loggerBridge.LogInformation(message.Trim());
    }
}

async Task HandleRunMessage(WorkerMessage message)
{
    var solution = JsonSerializer.Deserialize<Apollo.Contracts.Solutions.Solution>(message.Payload);
    var references = new List<MetadataReference>
    {
        await resolver.GetMetadataReferenceAsync("System.Private.CoreLib.wasm"),
        await resolver.GetMetadataReferenceAsync("System.Runtime.wasm"),
        await resolver.GetMetadataReferenceAsync("System.Console.wasm"),
        await resolver.GetMetadataReferenceAsync("System.Linq.wasm"),
        await resolver.GetMetadataReferenceAsync("System.Collections.wasm"),
        await resolver.GetMetadataReferenceAsync("System.Text.Json.wasm"),
        await resolver.GetMetadataReferenceAsync("xunit.assert.wasm"),
        await resolver.GetMetadataReferenceAsync("xunit.core.wasm"),
        await resolver.GetMetadataReferenceAsync("Apollo.Hosting.wasm"),
        await resolver.GetMetadataReferenceAsync("Microsoft.Extensions.DependencyInjection.Abstractions.wasm"),
    };
    
    var nugetRefs = solution.NuGetReferences ?? [];
    
    foreach (var nugetRef in nugetRefs)
    {
        if (nugetRef.AssemblyData?.Length > 0)
        {
            var nugetReference = MetadataReference.CreateFromImage(nugetRef.AssemblyData);
            references.Add(nugetReference);
            loggerBridge.LogDebug($"Added NuGet reference: {nugetRef.AssemblyName}");
        }
    }
    
    _service = new HostingCompilationService();
    loggerBridge.LogTrace("Compiling solution " + solution.Name);
    if (solution.Type != ProjectType.WebApi)
    {
        loggerBridge.LogWarning("Solution is not identified as a web api project: " + solution.Type);
    }
    loggerBridge.LogTrace($"Compiling code:\n{solution.Items.First().Content}");
    var result = _service.Compile(solution.Items.First().Content, references);
    
    loggerBridge.LogTrace("Compiled solution " + result.Success + " " + result.Assembly?.FullName);

    foreach (var diag in result?.Diagnostics ?? [])
    {
        loggerBridge.LogInformation(diag);
    }

    // Load NuGet assemblies into runtime before execution and register for resolution
    foreach (var nugetRef in nugetRefs)
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
                loggerBridge.LogDebug($"Loaded NuGet assembly for runtime: {nugetRef.AssemblyName} ({asmName})");
            }
            catch (Exception ex)
            {
                loggerBridge.LogWarning($"Failed to load NuGet assembly {nugetRef.AssemblyName}: {ex.Message}");
            }
        }
    }

    var assembly = result?.Assembly;
    
    loggerBridge.LogTrace("Loaded assembly: " + assembly?.FullName);
    try
    {
        _service.Execute(
            assembly, 
            ExecutionLogCallback,
            msg => Imports.PostMessage(msg.ToSerialized()),
            CancellationToken.None
        );
        _currentApp = _service.CurrentApp;
    }
    catch(Exception ex)
    {
        loggerBridge.LogError(ex.Message);
    }
}

async Task HandleRouteMessage(WorkerMessage message)
{
    loggerBridge.LogDebug($"Route message payload: {message.Payload}");
    var request = JsonSerializer.Deserialize<RouteRequest>(message.Payload);
    if (request != null)
    {
        try
        {
            if (WebApplication.Current == null)
            {
                loggerBridge.LogError("WebApplication.Current is null - API not running");
                await SendResponse(request.RequestId ?? "", "API not running", 503);
                return;
            }
            
            var response = WebApplication.Current.HandleRequest(request.Route, request.Content, request.Method);
            await SendResponse(request.RequestId ?? "", response, 200);
        }
        catch (Exception ex)
        {
            loggerBridge.LogError($"Error handling route: {ex.Message}");
            await SendResponse(request.RequestId ?? "", ex.Message, 500);
        }
    }
}

async Task SendError(string errorMessage)
{
    var msg = new WorkerMessage
    {
        Action = StandardWorkerActions.Error,
        Payload = errorMessage
    };
    Imports.PostMessage(msg.ToSerialized());
}

async Task SendResponse(string requestId, string body, int statusCode)
{
    var response = new RouteResponse(requestId, body, statusCode);
    var msg = new WorkerMessage
    {
        Action = WorkerActions.RouteResponse,
        Payload = JsonSerializer.Serialize(response)
    };
    Imports.PostMessage(msg.ToSerialized());
}