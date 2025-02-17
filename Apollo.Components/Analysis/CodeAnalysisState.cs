using System.Text.Json;
using Apollo.Components.Console;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions;
using Apollo.Contracts.Analysis;
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
    private bool _disabled;
    
    public event Func<Task>? OnCodeAnalysisStateChanged;
    private bool _workerReady = false;

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
        if(OnCodeAnalysisStateChanged?.GetInvocationList()?.Length > 0)
            await OnCodeAnalysisStateChanged?.Invoke();
    }

    private ICodeAnalysisWorker? _workerProxy;
    
    public CodeAnalysisState(ICodeAnalysisWorkerFactory workerFactory, CodeAnalysisConsoleService console,
        IMessageBus messageBus, SolutionsState solutionsState)
    {
        _workerFactory = workerFactory;
        _console = console;
        _messageBus = messageBus;
        _solutionsState = solutionsState;
    }

    public async Task<CompletionResponse?> GetCompletionAsync(string code, string completion)
    {
        if (Disabled || !_workerReady || string.IsNullOrWhiteSpace(code))
            return null;
        
        var bytes = await _workerProxy.GetCompletionAsync(code, completion);
        if (bytes == null || bytes.Length == 0)
            return null;

        try 
        {
            var response = JsonSerializer.Deserialize<ResponsePayload>(
                System.Text.Encoding.UTF8.GetString(bytes),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
            );

            if (response?.Payload == null)
                return null;
            
            var completionResponse = JsonSerializer.Deserialize<CompletionResponseWrapper>(
                response.Payload.ToString(), 
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
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
        if (Disabled || !_workerReady || solution == null)
            return [];

        var request = new DiagnosticRequestWrapper()
        {
            Solution = solution,
            Uri = uri,
        };
        
        byte[]? b = await _workerProxy.GetDiagnosticsAsync(JsonSerializer.Serialize(request));

        if (b == null || b.Length == 0)
            return [];
        
        var diagnostics = JsonSerializer.Deserialize<DiagnosticsResponseWrapper>(b, new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }).Payload;
        
        return diagnostics.ToList();
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

        await NotifyCodeAnalysisStatusChanged();
    }
    
    private Task HandleError(string error)
    {
        _console.AddError(error);
        return Task.CompletedTask;
    }

    private async Task HandleLog(LogMessage log)
    {
        if(!_workerReady && log.Message.StartsWith("Analysis worker ready"))
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