using System.Diagnostics;
using System.Reflection;
using Apollo.Components.Console;
using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions;
using Apollo.Components.Solutions.Events;
using Apollo.Contracts.Compilation;
using Microsoft.VisualStudio.TestPlatform.Utilities;

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
    public event Func<Task>? OnCompilerStatusChanged;
    private bool _workerReady = false;

    public const int MaxExecutionTime = 10;
    
    public bool Initialized => _workerReady;
    
    public bool CompilerReady => Status is CompilerStatus.Idle && Initialized;

    private async Task NotifyCompilerStatusChanged()
    {
        if(OnCompilerStatusChanged != null)
            await OnCompilerStatusChanged?.Invoke();
    }

    public CompilerStatus Status { get; private set; } = CompilerStatus.Uninitialized;

    private ICompilerWorker? _workerProxy;

    private Stopwatch _stopwatch = Stopwatch.StartNew();

    public CompilerState(ICompilerWorkerFactory compilerWorkerFactory, ConsoleOutputService console,
        IMessageBus messageBus)
    {
        _compilerWorkerFactory = compilerWorkerFactory;
        _console = console;
        _messageBus = messageBus;

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
            await Task.Delay(100);
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

        if (Status != CompilerStatus.Idle)
            return;
        
        await FocusConsoleTabAsync();

        Status = CompilerStatus.Executing;
        await NotifyCompilerStatusChanged();
        
        // Register a handler for the build completion, which leads to execution
        _workerProxy.OnCompileCompleted(RunBuildAsync);

        // Start the build process
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
    // Handle the build completion
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

    // Register the execution completion handler
    _workerProxy.OnExecuteCompleted(async executionResult =>
    {
        if (executionCompleted) return; // Prevent duplicate invocations
        executionCompleted = true;

            
        await cancellationTokenSource?.CancelAsync();
        
        await HandleExecuteCompleted(executionResult);
    });

    _stopwatch.Restart();

    try
    {
        // Start execution in the worker
        await _workerProxy.RequestExecuteAsync(result.Assembly);

        // Wait for either execution completion or timeout
        try
        {
            cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(MaxExecutionTime));
            await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when the token is canceled
        }

        if (!executionCompleted)
        {
            // Timeout occurred
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