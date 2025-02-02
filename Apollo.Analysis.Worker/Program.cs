using System;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Apollo.Analysis;
using Apollo.Analysis.Worker;
using Apollo.Contracts.Analysis;
using Apollo.Contracts.Solutions;
using Apollo.Infrastructure;
using Apollo.Infrastructure.Workers;
using KristofferStrube.Blazor.WebWorkers;
using Microsoft.CodeAnalysis;
using OmniSharp.Models.Diagnostics;
using OmniSharp.Models.v1.Completion;
using Solution = Apollo.Contracts.Solutions.Solution;

if (!OperatingSystem.IsBrowser())
    throw new PlatformNotSupportedException("Can only be run in the browser!");

Imports.PostMessage(
    WorkerLogWriter.DebugMessage($"Starting up analysis worker with base URI: {HostAddress.BaseUri}")
);

var resolver = new MetadataReferenceResourceProvider(HostAddress.BaseUri);
var loggerBridge = new AnalysisLogger();

var monacoService = new MonacoService(resolver, loggerBridge);

try
{
    string defaultUri = "";
    await monacoService.Init(defaultUri);
}
catch (Exception ex)
{
    Imports.PostMessage(ErrorWriter.SerializeErrorToWorkerMessage(ex.Message));
    throw;
}

bool keepRunning = true;

string _currentFileUri = string.Empty;
string _currentContent = string.Empty;

Imports.RegisterOnMessage(async e =>
{
    var payload = e.GetPropertyAsString("data");
    
    var message = JsonSerializer.Deserialize<WorkerMessage>(payload);
    
    try
    {
       
        switch (message.Action.ToLower())
        {
            case "get_completion":
                var wrapper = JsonSerializer.Deserialize<CompletionRequestWrapper>(message.Payload);
                ArgumentException.ThrowIfNullOrWhiteSpace(wrapper?.Code);
                ArgumentNullException.ThrowIfNull(wrapper?.Request);
                var completion = await monacoService.GetCompletionAsync(wrapper.Code,  wrapper.Request);

                var completionResponse = new WorkerMessage()
                {
                    Action = "completion_response",
                    Payload = Convert.ToBase64String(completion),
                };
                
                Imports.PostMessage(completionResponse.ToSerialized());
                break;

            case "get_completion_resolve":
               
                break;
            case "get_quick_hint":
                break;
            case "get_diagnostics":
                try 
                {
                    // First deserialize the worker message payload as a string
                    var solutionPayloadJson = message.Payload;
                    loggerBridge.LogTrace($"Received payload: {solutionPayloadJson}");
                    
                    // Then deserialize that into our solution
                    var request = JsonSerializer.Deserialize<DiagnosticRequestWrapper>(solutionPayloadJson);
                    if (request?.Solution == null)
                    {
                        loggerBridge.LogTrace("Failed to deserialize solution");
                        break;
                    }

                    // Create a new solution with clean items
                    var cleanSolution = new Solution 
                    {
                        Name = request.Solution.Name,
                        Items = request.Solution.Items.Select(item => new SolutionItem 
                        {
                            Path = item.Path,
                            Content = item.Content
                        }).ToList()
                    };
                    
                    // Get the file that triggered the diagnostics request
                    var currentFile = cleanSolution.Items.FirstOrDefault(i => i.Path == request.Uri);
                    
                    loggerBridge.LogTrace($"Received solution with {cleanSolution.Items.Count} files");
                    foreach (var item in cleanSolution.Items)
                    {
                        loggerBridge.LogTrace($"File: {item.Path}");
                        loggerBridge.LogTrace($"Content: {item.Content}");
                    }
                    
                    if (currentFile?.Path != _currentFileUri || currentFile?.Content != _currentContent)
                    {
                        _currentFileUri = currentFile?.Path ?? string.Empty;
                        _currentContent = currentFile?.Content ?? string.Empty;
                        
                        loggerBridge.LogTrace($"Analyzing file {_currentFileUri}");
                        var diag = await monacoService.GetDiagnosticsAsync(_currentFileUri, cleanSolution);

                        var diagnosticsResponse = new WorkerMessage()
                        {
                            Action = "diagnostics_response",
                            Payload = Convert.ToBase64String(diag),
                        };
                        
                        Imports.PostMessage(diagnosticsResponse.ToSerialized());
                    }
                    else
                    {
                        loggerBridge.LogWarning($"File mismatch: {currentFile?.Path} - {_currentFileUri}, content: {currentFile?.Content} - {_currentContent}");
                    }
                }
                catch (Exception ex)
                {
                    loggerBridge.LogTrace($"Error processing diagnostics: {ex.Message}");
                    throw;
                }
                break;
        }
    }
    catch (Exception ex)
    {
        Imports.PostMessage(ErrorWriter.SerializeErrorToWorkerMessage(ex));
    }
});

Imports.PostMessage(
    WorkerLogWriter.DebugMessage("Analysis worker ready")
);

while (keepRunning)
    await Task.Delay(100);
