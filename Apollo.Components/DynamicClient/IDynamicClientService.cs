using Apollo.Components.Solutions;
using Apollo.Contracts.Hosting;

namespace Apollo.Components.DynamicClient;

public interface IDynamicClientService
{
    bool IsRunning { get; }
    IReadOnlyList<NetworkRequest> RequestLog { get; }
    
    event Func<Task>? OnStateChanged;
    event Action<NetworkRequest>? OnRequestLogged;
    
    Task StartAsync(SolutionModel solution, string? entryFile = null);
    Task StopAsync();
    Task<NetworkResponse> HandleRequestAsync(string method, string path, string? body);
    
    string BuildClientDocument(SolutionModel solution, string? entryFile = null);
    void ClearRequestLog();
}

public record NetworkRequest
{
    public int Id { get; init; }
    public DateTime Timestamp { get; init; }
    public string Method { get; init; } = "GET";
    public string Path { get; init; } = "/";
    public string? RequestBody { get; init; }
    public string? ResponseBody { get; set; }
    public int StatusCode { get; set; } = 200;
    public double DurationMs { get; set; }
    public bool IsComplete { get; set; }
}

public record NetworkResponse(int StatusCode, string Body, Dictionary<string, string>? Headers = null);

