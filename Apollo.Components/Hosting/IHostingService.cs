using System.Text.Json;
using Apollo.Components.Console;
using Apollo.Components.DynamicTabs.Commands;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Solutions;
using Apollo.Contracts.Hosting;
using Microsoft.AspNetCore.Components;

namespace Apollo.Components.Hosting;

public interface IHostingService
{
    public bool Hosting { get; }
    public List<RouteViewModel>? Routes { get; }
    public TimeSpan Uptime { get; }
    public event Func<Task>? OnHostingStateChanged;
    
    public event Func<Task> OnRoutesChanged;
    public event Action<TimeSpan>? OnUptimeChanged;
    public HostingStates State { get; }
    public Task RunAsync(SolutionModel solution);

    public Task StopAsync();

    public Task<string> SendAsync(HttpMethodType method, string path, string? content = default);
}

public class HostingService : IHostingService
{
    private readonly IHostingWorkerFactory _factory;
    private readonly WebHostConsoleService _console;
    private readonly IMessageBus _bus;
    private IHostingWorker? _hostingWorker;
    private System.Timers.Timer? _uptimeTimer;
    private DateTime _startTime;
    
    private bool _workerReady;
    
    private IReadOnlyList<RouteViewModel>? _routes = default;

    public List<RouteViewModel>? Routes => _routes?.ToList();

    public bool Hosting => State == HostingStates.Hosting;

    public event Func<Task>? OnHostingStateChanged;
    
    public event Func<Task>? OnRoutesChanged;
    
    public TimeSpan Uptime { get; private set; }
    public event Action<TimeSpan>? OnUptimeChanged;

    protected async Task NotifyStateChangedAsync()
    {
        if(OnHostingStateChanged != null)
            await OnHostingStateChanged.Invoke();
    }

    protected async Task NotifyRoutesChangedAsync()
    {
        if(_routes?.Any() == true) 
            await _bus.PublishAsync(new FocusTab("Web Api"));
        
        if(OnRoutesChanged != null)
            await OnRoutesChanged.Invoke();
    }

    public HostingService(IHostingWorkerFactory factory, WebHostConsoleService console, IMessageBus bus)
    {
        _factory = factory;
        _console = console;
        _bus = bus;
        State = HostingStates.Uninitialized;
    }

    public HostingStates State { get; set; }

    public async Task RunAsync(SolutionModel solution)
    {
        _hostingWorker ??= await InitializeWorkerAsync();

        State = HostingStates.Hosting;
        _startTime = DateTime.UtcNow;
        StartUptimeTimer();
        
        await NotifyStateChangedAsync();
        await _hostingWorker.RunAsync(JsonSerializer.Serialize(solution.ToContract()));
    }

    public async Task StopAsync()
    {
        if (State != HostingStates.Hosting)
            return;

        StopUptimeTimer();
        
        if (_hostingWorker == null)
            return;

        await _hostingWorker.StopAsync();
        State = HostingStates.Initialized;
        
        await NotifyStateChangedAsync();
        
        /*
        await _hostingWorker.TerminateAsync();
        _hostingWorker = null;
        State = HostingStates.Uninitialized;
        
        await NotifyStateChangedAsync();
        */
    }
    

    private async Task<IHostingWorker> InitializeWorkerAsync()
    {
        State = HostingStates.Initializing;
        await _bus.PublishAsync(new FocusTab("Web Host"));
        _hostingWorker = await _factory.CreateAsync(CancellationToken.None);
        _hostingWorker.OnError(s =>
        {
            _console.AddError(s);
            return Task.CompletedTask;
        });
        _hostingWorker.OnLog(s =>
        {
            if (!_workerReady && s.Message.Contains("Hosting worker ready"))
            {
                _workerReady = true;
                State = HostingStates.Initialized;
            }
            
            _console.AddLog(s.Message, (ConsoleSeverity)s.Severity);
            return Task.CompletedTask;
        });
        
        _hostingWorker.OnRoutesDiscovered(HandleRoutesDiscovered);
        
        _console.AddDebug("Host service started worker");
        
        while(!_workerReady)
            await Task.Delay(100);
        
        return _hostingWorker;
    }

    protected async Task HandleRoutesDiscovered(IReadOnlyList<RouteInfo> routes)
    {
        //_console.AddDebug($"Routes: {JsonSerializer.Serialize(routes)}");
        _console.AddInfo($"Discovered {routes.Count} routes");
        _routes = routes.Select(x => x.ToViewModel()).ToList();
        await NotifyRoutesChangedAsync();
    }

    public async Task<string> SendAsync(HttpMethodType method, string path, string? content = default)
    {
        if (_hostingWorker == null || !Hosting)
        {
            _console.AddWarning("Cannot send request - host not running");
            return "Host not running";
        }

        _console.AddLog($"Sending {method} request to {path}", ConsoleSeverity.Info);
        
        try 
        {
            await _hostingWorker.SendAsync(method, path, content);
            _console.AddSuccess($"Request sent to {path}");
            return "Request sent successfully";
        }
        catch (Exception ex)
        {
            _console.AddError($"Error sending request: {ex.Message}");
            return $"Error: {ex.Message}";
        }
    }

    private void StartUptimeTimer()
    {
        _uptimeTimer?.Dispose();
        _uptimeTimer = new System.Timers.Timer(1000); // Update every second
        _uptimeTimer.Elapsed += (s, e) =>
        {
            Uptime = DateTime.UtcNow - _startTime;
            OnUptimeChanged?.Invoke(Uptime);
        };
        _uptimeTimer.Start();
    }

    private void StopUptimeTimer()
    {
        if (_uptimeTimer != null)
        {
            _uptimeTimer.Stop();
            _uptimeTimer.Dispose();
            _uptimeTimer = null;
        }
        Uptime = TimeSpan.Zero;
        OnUptimeChanged?.Invoke(Uptime);
    }
}
