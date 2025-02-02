using Apollo.Hosting.Logging;
using Apollo.Infrastructure.Workers;

namespace Apollo.Hosting;

public static class CompilationContext
{
    private static HostingConsoleService? _console;
    private static Action<WorkerMessage>? _messageSender;
    private static string? _requestBody;
    
    public static void SetConsole(HostingConsoleService console)
    {
        _console = console;
    }
    
    public static HostingConsoleService? GetConsole()
    {
        return _console;
    }

    public static void SetMessageSender(Action<WorkerMessage> sender)
    {
        _messageSender = sender;
    }
    
    public static void SendMessage(WorkerMessage message)
    {
        _messageSender?.Invoke(message);
    }

    public static void SetRequestBody(string? body)
    {
        _requestBody = body;
    }

    public static string? GetRequestBody()
    {
        return _requestBody;
    }
} 