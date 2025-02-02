using Apollo.Components.Settings;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Apollo.Components.Console;

public abstract class BaseConsoleService
{
    private List<ConsoleOutputViewModel> _internalErrorLog = [];
    
    public IScrollManager? ScrollManager { get; set; } = default!;
    
    public IJsApiService JsApiService { get; set; } = default!;

    protected BaseConsoleService(IJsApiService jsApiService, IScrollManager? scrollManager = null)
    {
        JsApiService = jsApiService;
        ScrollManager = scrollManager;
    }

    private readonly List<ConsoleOutputViewModel> _logs = [];
    
    public bool AutoScroll { get; set; } = true;

    public bool LogEnabledOnly { get; set; } = false;
    public IReadOnlyList<ConsoleOutputViewModel> Logs => _logs.AsReadOnly();
    public event Action<ConsoleOutputViewModel>? OnConsoleOutputReceived;
    public event Action? OnConsoleCleared;
    
    public virtual IReadOnlyList<ConsoleSeverity> SupportedSeverities => [ConsoleSeverity.Debug, ConsoleSeverity.Trace,  ConsoleSeverity.Error, ConsoleSeverity.Warning, ConsoleSeverity.Info, ConsoleSeverity.Success ];

    public virtual IReadOnlyCollection<string> Selected { get; set; } = ["Debug", "Trace", "Information", "Warning", "Error", "Success"];

    public async Task AddLogAsync(string message, ConsoleSeverity severity)
    {
        if (LogEnabledOnly && !Selected.Any(x => !string.IsNullOrWhiteSpace(x) && x.ToConsoleSeverity().Equals(severity)))
            return;
            
        var logEntry = new ConsoleOutputViewModel
        {
            Timestamp = DateTimeOffset.Now,
            Severity = severity,
            Message = message
        };
        
        _logs.Add(logEntry);
        
        if(OnConsoleOutputReceived != null)
            OnConsoleOutputReceived?.Invoke(logEntry);
        
        await TryScrollToLogAsync(logEntry);
    }
    
    public async void AddLog(string message, ConsoleSeverity severity)
    {
        await AddLogAsync(message, severity);
    }
    
    public void AddDebug(string message) => AddLog(message, ConsoleSeverity.Debug);
        
    public void AddTrace(string message) => AddLog(message, ConsoleSeverity.Trace);

    public void AddInfo(string message) => AddLog(message, ConsoleSeverity.Info);

    public void AddWarning(string message) => AddLog(message, ConsoleSeverity.Warning);

    public void AddError(string message) => AddLog(message, ConsoleSeverity.Error);

    public void AddSuccess(string message) => AddLog(message, ConsoleSeverity.Success);

    private async Task TryScrollToLogAsync(ConsoleOutputViewModel log)
    {
        try
        {
            if (AutoScroll && ScrollManager != null)
                await ScrollManager.ScrollToListItemAsync(log.GetHtmlId());
        }
        catch(Exception ex)
        {
            _internalErrorLog.Add(new ConsoleOutputViewModel()
            {
                Timestamp = DateTimeOffset.Now,
                Severity = ConsoleSeverity.Error,
                Message = ex.Message
            });
        }
    }
    
    public List<ConsoleOutputViewModel> Filter(IEnumerable<ConsoleOutputViewModel> logs)
    {
        var selectedSeverities = Selected
            .Select(s => Enum.TryParse<ConsoleSeverity>(s, true, out var severity) ? severity : (ConsoleSeverity?)null)
            .Where(severity => severity.HasValue)
            .Select(severity => severity.Value)
            .ToHashSet();

        return logs
            .Where(log => selectedSeverities.Contains(log.Severity))
            .ToList();
    }

    public void ClearConsole()
    {
        _logs.Clear();
        OnConsoleCleared?.Invoke();
    }

    public MudBlazor.Color GetSeverityColor(ConsoleSeverity severity) =>
        severity switch
        {
            ConsoleSeverity.Debug => MudBlazor.Color.Inherit,
            ConsoleSeverity.Trace => MudBlazor.Color.Inherit,
            ConsoleSeverity.Info => MudBlazor.Color.Info,
            ConsoleSeverity.Warning => MudBlazor.Color.Warning,
            ConsoleSeverity.Error => MudBlazor.Color.Error,
            ConsoleSeverity.Success => MudBlazor.Color.Success,
            _ => MudBlazor.Color.Default
        };

    public string LogTextClass(ConsoleSeverity severity) =>
        severity switch
        {
            ConsoleSeverity.Debug => $"mud-text-secondary",
            ConsoleSeverity.Trace => $"apollo-console-trace",
            _ => ""
        };

    public async Task CopyLogsAsync()
    {
        await JsApiService.CopyToClipboardAsync(string.Join(Environment.NewLine, Logs.Select(l => l.Message)));
    }

    public async Task CopyVisibleLogsAsync()
    {
        await JsApiService.CopyToClipboardAsync(string.Join(Environment.NewLine, Filter(Logs).Select(l => l.Message.ToString())));
    }
}