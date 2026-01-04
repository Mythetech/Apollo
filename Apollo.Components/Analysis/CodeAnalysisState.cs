using System.Text.Json;
using Apollo.Components.Console;
using Mythetech.Framework.Infrastructure.MessageBus;
using Apollo.Components.NuGet;
using Apollo.Components.Solutions;
using Apollo.Contracts.Analysis;
using Apollo.Contracts.Solutions;
using Apollo.Contracts.Workers;
using OmniSharp.Models.v1.Completion;
using Solution = Apollo.Contracts.Solutions.Solution;

namespace Apollo.Components.Analysis;

public class CodeAnalysisState
{
    private readonly ICodeAnalysisWorkerFactory _workerFactory;
    private readonly CodeAnalysisConsoleService _console;
    private readonly IMessageBus _messageBus;
    private readonly SolutionsState _solutionsState;
    private readonly UserAssemblyStore _userAssemblyStore;
    private readonly NuGetState _nuGetState;
    private readonly INuGetStorageService _nuGetStorageService;
    private bool _disabled;
    
    public event Func<Task>? OnCodeAnalysisStateChanged;
    private bool _workerReady = false;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public bool Disabled
    {
        get => _disabled;
        set
        {
            if (_disabled == value) return;
            _disabled = value;
            _ = HandleDisabledChanged();
        }
    }

    private async Task HandleDisabledChanged()
    {
        if (_disabled)
        {
            await TerminateWorkerAsync();
            _console.AddInfo("Code analysis disabled");
        }
        else
        {
            await StartAsync();
            _console.AddInfo("Code analysis enabled");
        }
        await NotifyCodeAnalysisStatusChanged();
    }

    private async Task NotifyCodeAnalysisStatusChanged()
    {
        if (OnCodeAnalysisStateChanged?.GetInvocationList()?.Length > 0)
            await OnCodeAnalysisStateChanged?.Invoke()!;
    }

    private ICodeAnalysisWorker? _workerProxy;
    
    public CodeAnalysisState(
        ICodeAnalysisWorkerFactory workerFactory, 
        CodeAnalysisConsoleService console,
        IMessageBus messageBus, 
        SolutionsState solutionsState,
        UserAssemblyStore userAssemblyStore,
        NuGetState nuGetState,
        INuGetStorageService nuGetStorageService)
    {
        _workerFactory = workerFactory;
        _console = console;
        _messageBus = messageBus;
        _solutionsState = solutionsState;
        _userAssemblyStore = userAssemblyStore;
        _nuGetState = nuGetState;
        _nuGetStorageService = nuGetStorageService;
        
        _userAssemblyStore.OnAssemblyUpdated += HandleUserAssemblyUpdated;
    }

    private async Task HandleUserAssemblyUpdated(byte[] assemblyBytes)
    {
        if (!_workerReady || _workerProxy == null) return;
        
        try
        {
            var request = new UserAssemblyUpdateRequest { AssemblyBytes = assemblyBytes };
            await _workerProxy.UpdateUserAssemblyAsync(JsonSerializer.Serialize(request, JsonOptions));
            _console.AddInfo("User assembly updated for intellisense");
        }
        catch (Exception ex)
        {
            _console.AddError($"Failed to update user assembly for intellisense: {ex.Message}");
        }
    }

    public async Task<CompletionResponse?> GetCompletionAsync(string code, string completion)
    {
        if (Disabled || !_workerReady || string.IsNullOrWhiteSpace(code))
            return null;
        
        var bytes = await _workerProxy!.GetCompletionAsync(code, completion);
        if (bytes == null || bytes.Length == 0)
            return null;

        try 
        {
            var response = JsonSerializer.Deserialize<ResponsePayload>(
                System.Text.Encoding.UTF8.GetString(bytes),
                JsonOptions
            );

            if (response?.Payload == null)
                return null;
            
            var completionResponse = JsonSerializer.Deserialize<CompletionResponseWrapper>(
                response.Payload.ToString()!, 
                JsonOptions
            );

            return completionResponse?.Payload;
        }
        catch (Exception ex)
        {
            _console.AddError($"Error deserializing completion response: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Diagnostic>> GetDiagnosticsAsync(Solution solution, string uri)
    {
        if (Disabled || !_workerReady || solution == null || _workerProxy == null)
            return [];

        solution.NuGetReferences = await LoadNuGetReferencesAsync();
        
        var request = new DiagnosticRequestWrapper
        {
            Solution = solution,
            Uri = uri,
        };
        
        byte[]? bytes = await _workerProxy.GetDiagnosticsAsync(JsonSerializer.Serialize(request));

        if (bytes == null || bytes.Length == 0)
            return [];
        
        var diagnostics = JsonSerializer.Deserialize<DiagnosticsResponseWrapper>(bytes, JsonOptions)?.Payload;
        
        return diagnostics?.ToList() ?? [];
    }
    
    private async Task<List<NuGetReference>> LoadNuGetReferencesAsync()
    {
        var references = new List<NuGetReference>();
        
        foreach (var package in _nuGetState.InstalledPackages)
        {
            foreach (var assemblyName in package.AssemblyNames)
            {
                var assemblyData = await _nuGetStorageService.GetAssemblyDataAsync(
                    package.Id, 
                    package.Version, 
                    assemblyName);
                    
                if (assemblyData != null)
                {
                    references.Add(new NuGetReference
                    {
                        PackageId = package.Id,
                        AssemblyName = assemblyName,
                        AssemblyData = assemblyData
                    });
                    _console.AddLog($"Loaded NuGet assembly: {assemblyName} from {package.Id}", ConsoleSeverity.Debug);
                }
            }
        }
        
        return references;
    }

    public async Task UpdateDocumentAsync(string path, string fullContent)
    {
        if (Disabled || !_workerReady || _workerProxy == null) return;

        var request = new DocumentUpdateRequest
        {
            Path = path,
            IsFullContent = true,
            FullContent = fullContent
        };

        try
        {
            await _workerProxy.UpdateDocumentAsync(JsonSerializer.Serialize(request, JsonOptions));
        }
        catch (Exception ex)
        {
            _console.AddError($"Error updating document: {ex.Message}");
        }
    }

    public async Task ApplyTextChangesAsync(string path, List<TextChangeInfo> changes)
    {
        if (Disabled || !_workerReady || _workerProxy == null) return;

        var request = new DocumentUpdateRequest
        {
            Path = path,
            IsFullContent = false,
            Changes = changes
        };

        try
        {
            await _workerProxy.UpdateDocumentAsync(JsonSerializer.Serialize(request, JsonOptions));
        }
        catch (Exception ex)
        {
            _console.AddError($"Error applying text changes: {ex.Message}");
        }
    }

    public async Task SetCurrentDocumentAsync(string path)
    {
        if (Disabled || !_workerReady || _workerProxy == null) return;

        var request = new SetCurrentDocumentRequest { Path = path };

        try
        {
            await _workerProxy.SetCurrentDocumentAsync(JsonSerializer.Serialize(request, JsonOptions));
        }
        catch (Exception ex)
        {
            _console.AddError($"Error setting current document: {ex.Message}");
        }
    }

    public async Task UpdateUserAssemblyAsync(byte[]? assemblyBytes)
    {
        if (Disabled || !_workerReady || _workerProxy == null) return;

        var request = new UserAssemblyUpdateRequest { AssemblyBytes = assemblyBytes };

        try
        {
            await _workerProxy.UpdateUserAssemblyAsync(JsonSerializer.Serialize(request, JsonOptions));
            _console.AddTrace("User assembly reference sent to analysis worker");
        }
        catch (Exception ex)
        {
            _console.AddError($"Error updating user assembly: {ex.Message}");
        }
    }

    public async Task StartAsync()
    {
        if (Disabled || _workerProxy != null)
            return;
        
        _workerProxy = await _workerFactory.CreateAsync();

        _workerProxy.OnError(HandleError);
        _workerProxy.OnLog(HandleLog);

        while (!_workerReady)
        {
            await Task.Yield();
            await Task.Delay(100);
        }

        if (_userAssemblyStore.HasAssembly)
        {
            await UpdateUserAssemblyAsync(_userAssemblyStore.CurrentAssembly);
        }

        await NotifyCodeAnalysisStatusChanged();
    }
    
    private Task HandleError(string error)
    {
        _console.AddError(error);
        return Task.CompletedTask;
    }

    private async Task HandleLog(LogMessage log)
    {
        if (!_workerReady && log.Message.StartsWith("Analysis worker ready"))
        {
            _workerReady = true;
            await NotifyCodeAnalysisStatusChanged();
        }
        
        _console.AddLog(log.Message, (ConsoleSeverity)log.Severity);
    }
    
    private async Task TerminateWorkerAsync()
    {
        if (_workerProxy != null)
        {
            await _workerProxy.TerminateAsync();
            _workerProxy = null;
            _workerReady = false;
        }
    }
}
