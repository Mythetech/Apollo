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
                var completion = await monacoService.GetCompletionAsync(wrapper.Code, wrapper.Request);

                var completionResponse = new WorkerMessage()
                {
                    Action = "completion_response",
                    Payload = Convert.ToBase64String(completion),
                };
                
                Imports.PostMessage(completionResponse.ToSerialized());
                break;

            case "get_completion_resolve":
                var resolveCompletion = await monacoService.GetCompletionResolveAsync(message.Payload);

                var resolveCompletionResponse = new WorkerMessage()
                {
                    Action = "completion_resolve_response",
                    Payload = Convert.ToBase64String(resolveCompletion),
                };
                
                Imports.PostMessage(resolveCompletionResponse.ToSerialized());
                break;
                
            case "get_quick_hint":
                var quickHint = await monacoService.GetQuickInfoAsync(message.Payload);

                var quickHintResponse = new WorkerMessage()
                {
                    Action = "quick_hint_response",
                    Payload = Convert.ToBase64String(quickHint),
                };
                
                Imports.PostMessage(quickHintResponse.ToSerialized());
                break;
                
            case "get_diagnostics":
                try 
                {
                    var solutionPayloadJson = message.Payload;
                    loggerBridge.LogDebug($"Received payload: {solutionPayloadJson}");
                    
                    var request = JsonSerializer.Deserialize<DiagnosticRequestWrapper>(solutionPayloadJson);
                    if (request?.Solution == null)
                    {
                        loggerBridge.LogTrace("Failed to deserialize solution");
                        break;
                    }

                    var cleanSolution = new Solution 
                    {
                        Name = request.Solution.Name,
                        Items = request.Solution.Items.Select(item => new SolutionItem 
                        {
                            Path = item.Path,
                            Content = item.Content
                        }).ToList()
                    };
                    
                    var currentFile = cleanSolution.Items.FirstOrDefault(i => i.Path == request.Uri);
                    
                    loggerBridge.LogDebug($"Received solution with {cleanSolution.Items.Count} files");
                    foreach (var item in cleanSolution.Items)
                    {
                        loggerBridge.LogDebug($"File: {item.Path}");
                        loggerBridge.LogDebug($"Content: {item.Content}");
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
                }
                catch (Exception ex)
                {
                    loggerBridge.LogTrace($"Error processing diagnostics: {ex.Message}");
                    throw;
                }
                break;
                
            case "update_document":
                try
                {
                    var updateResult = await monacoService.HandleDocumentUpdateAsync(message.Payload);
                    var updateResponse = new WorkerMessage
                    {
                        Action = "document_update_response",
                        Payload = Convert.ToBase64String(updateResult)
                    };
                    Imports.PostMessage(updateResponse.ToSerialized());
                }
                catch (Exception ex)
                {
                    loggerBridge.LogTrace($"Error updating document: {ex.Message}");
                }
                break;
                
            case "set_current_document":
                try
                {
                    var setDocResult = await monacoService.HandleSetCurrentDocumentAsync(message.Payload);
                    var setDocResponse = new WorkerMessage
                    {
                        Action = "set_current_document_response",
                        Payload = Convert.ToBase64String(setDocResult)
                    };
                    Imports.PostMessage(setDocResponse.ToSerialized());
                }
                catch (Exception ex)
                {
                    loggerBridge.LogTrace($"Error setting current document: {ex.Message}");
                }
                break;
                
            case "update_user_assembly":
                try
                {
                    loggerBridge.LogDebug("Received user assembly update request");
                    var assemblyResult = await monacoService.HandleUserAssemblyUpdateAsync(message.Payload);
                    var assemblyResponse = new WorkerMessage
                    {
                        Action = "user_assembly_update_response",
                        Payload = Convert.ToBase64String(assemblyResult)
                    };
                    Imports.PostMessage(assemblyResponse.ToSerialized());
                    loggerBridge.LogDebug("User assembly reference updated for intellisense");
                }
                catch (Exception ex)
                {
                    loggerBridge.LogTrace($"Error updating user assembly: {ex.Message}");
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
