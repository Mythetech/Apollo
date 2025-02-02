using Microsoft.Extensions.Logging;

namespace Apollo.Components.Infrastructure.Logging;

public class SystemLogger : ILogger
{
    private readonly string _categoryName;
    private readonly SystemLoggerProvider _provider;

    public SystemLogger(string categoryName, SystemLoggerProvider provider)
    {
        _categoryName = categoryName;
        _provider = provider;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _provider.AddLogEntry(new LogEntry(
            DateTime.Now,
            logLevel,
            _categoryName,
            formatter(state, exception),
            exception));
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable BeginScope<TState>(TState state) => default!;
}

public class SystemLoggerProvider : ILoggerProvider
{
    private readonly List<LogEntry> _logEntries = new();
    private readonly int _maxEntries;

    public event Action? OnLogEntryAdded;
    public IReadOnlyList<LogEntry> LogEntries => _logEntries;

    public SystemLoggerProvider(int maxEntries = 1000)
    {
        _maxEntries = maxEntries;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new SystemLogger(categoryName, this);
    }

    internal void AddLogEntry(LogEntry entry)
    {
        _logEntries.Add(entry);
        if (_logEntries.Count > _maxEntries)
            _logEntries.RemoveAt(0);
            
        OnLogEntryAdded?.Invoke();
    }

    public void Dispose()
    {
    }
}

public record LogEntry(DateTime Timestamp, LogLevel LogLevel, string Category, string Message, Exception? Exception); 