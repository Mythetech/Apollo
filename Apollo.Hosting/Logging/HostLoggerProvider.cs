namespace Apollo.Hosting.Logging;

public class HostLoggerProvider
{
    private static IHostLogger? _logger;
    
    private static readonly object _lock = new();
    
    public static IHostLogger Logger => _logger ?? throw new InvalidOperationException("Logger not initialized");

    public static void Initialize(IHostLogger logger)
    {
        lock (_lock)
        {
            if (_logger == null)
            {
                _logger = logger;
                _logger.LogTrace("HostLoggerProvider initialized");
            }
        }
    }

    public static bool IsInitialized => _logger != null;
} 