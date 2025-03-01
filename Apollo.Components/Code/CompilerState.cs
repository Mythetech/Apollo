using System.Diagnostics;
using System.Reflection;
using Apollo.Components.Console;
using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions;
using Apollo.Components.Solutions.Events;
using Apollo.Contracts.Compilation;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestPlatform.Utilities;
using MudBlazor.Extensions;
using Assembly = System.Reflection.Assembly;

namespace Apollo.Components.Code;

public enum CompilerStatus
{
    Uninitialized,
    Initializing,
    Idle,
    Building,
    Executing,
}

public class CompilerState
{
    private readonly ICompilerWorkerFactory _compilerWorkerFactory;
    private readonly ConsoleOutputService _console;
    private readonly IMessageBus _messageBus;
    private readonly ILogger<CompilerState> _logger;
    public event Func<Task>? OnCompilerStatusChanged;
    private bool _workerReady = false;

    public const int MaxExecutionTime = 10;
    
    public bool Initialized => _workerReady;
    
    public bool CompilerReady => Status is CompilerStatus.Idle && Initialized;

    private async Task NotifyCompilerStatusChanged()
    {
        if(OnCompilerStatusChanged != null)
            await OnCompilerStatusChanged?.Invoke()!;
    }

    public CompilerStatus Status { get; private set; } = CompilerStatus.Uninitialized;

    private ICompilerWorker? _workerProxy;

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    public CompilerState(ICompilerWorkerFactory compilerWorkerFactory, ConsoleOutputService console,
        IMessageBus messageBus, ILogger<CompilerState> logger)
    {
        _compilerWorkerFactory = compilerWorkerFactory;
        _console = console;
        _messageBus = messageBus;
        _logger = logger;

        StartInternal();
    }

    private async void StartInternal()
    {
        await StartAsync();
    }

    private async Task StartAsync()
    {
        if (_workerProxy != null)
            return;
        
        Status = CompilerStatus.Initializing;
        await NotifyCompilerStatusChanged();
        
        _workerProxy = await _compilerWorkerFactory.CreateAsync();

        _workerProxy.OnError(HandleError);

        _workerProxy.OnLog(HandleLog);

        while (!_workerReady)
        {
            await Task.Yield();
            await Task.Delay(50);
        }

        Status = CompilerStatus.Idle;
        await NotifyCompilerStatusChanged();
    }
    
    private async Task FocusConsoleTabAsync() => await _messageBus.PublishAsync(new FocusTab("Console Output"));
    
    public async Task BuildAsync(SolutionModel solution)
    {
        if (Status == CompilerStatus.Uninitialized)
        {
            await StartAsync();
        }

        await FocusConsoleTabAsync();

        if (Status != CompilerStatus.Idle)
            return;
        
        _workerProxy.OnCompileCompleted(HandleBuildComplete);

        await RequestBuildAsync(solution);
    }

    public async Task ExecuteAsync(SolutionModel solution)
    {
        if (Status == CompilerStatus.Uninitialized)
        {
            await StartAsync();
        }

        while (Status == CompilerStatus.Building)
        {
            await Task.Delay(50);
        }

        if (Status != CompilerStatus.Idle)
            return;
        
        await FocusConsoleTabAsync();

        Status = CompilerStatus.Executing;
        await NotifyCompilerStatusChanged();
        
        _workerProxy.OnCompileCompleted(RunBuildAsync);

        await RequestBuildAsync(solution);
    }

    private async Task RequestBuildAsync(SolutionModel solution)
    {
        Status = CompilerStatus.Building;
        await NotifyCompilerStatusChanged();
        
        _stopwatch.Restart();
        
        _console.AddLog("Requesting build", ConsoleSeverity.Debug);
        await _workerProxy.RequestBuildAsync(solution.ToContract());
    }

    private Task HandleError(string error)
    {
        _console.AddLog(error, ConsoleSeverity.Error);
        return Task.CompletedTask;
    }

    private async Task HandleLog(CompilerLog log)
    {
        if(!_workerReady && log.Message.StartsWith("Worker ready"))
        {
            _workerReady = true;
            await NotifyCompilerStatusChanged();
        }
        
        _console.AddLog(log.Message, (ConsoleSeverity)log.Severity);
    }

    private async Task HandleBuildComplete(CompilationReferenceResult result)
    {
        _logger.LogInformation($"Compilation result {result.Success}, {result.Assembly?.Length ?? 0} bytes, Diagnostics: {result?.Diagnostics?.Count}");
        
        _console.AddLog($"Build completed in {result.BuildTime.Milliseconds}ms", ConsoleSeverity.Debug);
        
        if (result.Success)
        {
            try
            {
                var asm = Assembly.Load(result.Assembly);
                await _messageBus.PublishAsync(new BuildCompleted(new CompilationResult(true, asm)));
            }
            catch (Exception ex)
            {
                _console.AddLog(ex.Message, ConsoleSeverity.Error);
                await _messageBus.PublishAsync(new BuildCompleted(new CompilationResult(false, default)));
            }
        }
        
        if (result.Assembly == null)
        {
            _console.AddLog("No assembly from build", ConsoleSeverity.Warning);
            foreach (string? log in result?.Diagnostics ?? [])
            {
                if(!string.IsNullOrWhiteSpace(log))
                    _console.AddLog(log, ConsoleSeverity.Info);
            }
        }

        Status = CompilerStatus.Idle;
        await NotifyCompilerStatusChanged();
    }

    private async Task RunBuildAsync(CompilationReferenceResult result)
{
    await HandleBuildComplete(result);

    if (result.Assembly == null)
    {
        return;
    }

    Status = CompilerStatus.Executing;
    await NotifyCompilerStatusChanged();

    _console.AddLog("Starting execution of the compiled assembly...", ConsoleSeverity.Debug);

    using var cancellationTokenSource = new CancellationTokenSource();
    var executionCompleted = false;

    _workerProxy.OnExecuteCompleted(async executionResult =>
    {
        if (executionCompleted) return; 
        executionCompleted = true;

            
        await cancellationTokenSource?.CancelAsync();
        
        await HandleExecuteCompleted(executionResult);
    });

    _stopwatch.Restart();

    try
    {
        await _workerProxy.RequestExecuteAsync(result.Assembly);

        try
        {
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(MaxExecutionTime));
            await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
        }

        if (!executionCompleted)
        {
            _console.AddLog($"Execution exceeded max time of {MaxExecutionTime}s. Terminating worker...", ConsoleSeverity.Error);
            await TerminateWorkerAsync();
            _console.AddLog("Worker terminated due to timeout.", ConsoleSeverity.Debug);
            Status = CompilerStatus.Uninitialized;
            await NotifyCompilerStatusChanged();
            await StartAsync();
        }
    }
    finally
    {
        if (_stopwatch.IsRunning)
        {
            _stopwatch.Stop();
        }
        
        Status = CompilerStatus.Idle;
        await NotifyCompilerStatusChanged();
    }
}

    private async Task HandleExecuteCompleted(ExecutionResult result)
    {
        if (result.Error)
        {
            _console.AddLog(string.Join(Environment.NewLine, result.Messages), ConsoleSeverity.Error);
        }
        else
        {
            _console.AddLog($"Worker completed in {result.ExecutionTime.Milliseconds}ms", ConsoleSeverity.Success);
        }
        
        _stopwatch.Stop();
        
        _console.AddLog($"Total request execution time {_stopwatch.ElapsedMilliseconds}ms", ConsoleSeverity.Debug);

        Status = CompilerStatus.Idle;
        await NotifyCompilerStatusChanged();
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