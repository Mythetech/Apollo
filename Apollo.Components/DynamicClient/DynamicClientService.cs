using System.Diagnostics;
using Apollo.Components.Hosting;
using Apollo.Components.Solutions;
using Apollo.Contracts.Hosting;

namespace Apollo.Components.DynamicClient;

public class DynamicClientService : IDynamicClientService
{
    private readonly IHostingService _hostingService;
    private readonly List<NetworkRequest> _requestLog = new();
    private int _requestCounter;
    
    public bool IsRunning { get; private set; }
    public IReadOnlyList<NetworkRequest> RequestLog => _requestLog.AsReadOnly();
    
    public event Func<Task>? OnStateChanged;
    public event Action<NetworkRequest>? OnRequestLogged;

    public DynamicClientService(IHostingService hostingService)
    {
        _hostingService = hostingService;
    }

    public async Task StartAsync(SolutionModel solution, string? entryFile = null)
    {
        if (!_hostingService.Hosting)
        {
            await _hostingService.RunAsync(solution);
        }
        
        IsRunning = true;
        _requestCounter = 0;
        _requestLog.Clear();
        
        if (OnStateChanged != null)
            await OnStateChanged.Invoke();
    }

    public async Task StopAsync()
    {
        IsRunning = false;
        
        if (OnStateChanged != null)
            await OnStateChanged.Invoke();
    }

    public async Task<NetworkResponse> HandleRequestAsync(string method, string path, string? body)
    {
        var request = new NetworkRequest
        {
            Id = ++_requestCounter,
            Timestamp = DateTime.UtcNow,
            Method = method.ToUpperInvariant(),
            Path = path,
            RequestBody = body
        };
        
        _requestLog.Add(request);
        OnRequestLogged?.Invoke(request);
        
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            if (!_hostingService.Hosting)
            {
                request.StatusCode = 503;
                request.ResponseBody = "API server not running";
                request.IsComplete = true;
                request.DurationMs = stopwatch.Elapsed.TotalMilliseconds;
                return new NetworkResponse(503, "API server not running");
            }

            var methodType = ParseHttpMethod(method);
            var response = await _hostingService.SendAsync(methodType, path, body);
            
            stopwatch.Stop();
            
            request.ResponseBody = response;
            request.StatusCode = 200;
            request.DurationMs = stopwatch.Elapsed.TotalMilliseconds;
            request.IsComplete = true;
            
            return new NetworkResponse(200, response);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            request.StatusCode = 500;
            request.ResponseBody = ex.Message;
            request.DurationMs = stopwatch.Elapsed.TotalMilliseconds;
            request.IsComplete = true;
            
            return new NetworkResponse(500, ex.Message);
        }
    }

    public string BuildClientDocument(SolutionModel solution, string? entryFile = null)
    {
        var htmlFile = entryFile != null
            ? solution.Files.FirstOrDefault(f => f.Name.Equals(entryFile, StringComparison.OrdinalIgnoreCase))
            : solution.Files.FirstOrDefault(f => 
                f.Name.Equals("index.html", StringComparison.OrdinalIgnoreCase) ||
                f.Uri.Contains("/client/", StringComparison.OrdinalIgnoreCase) && f.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase));

        if (htmlFile == null)
        {
            return BuildDefaultErrorDocument("No HTML file found. Create an index.html in your solution.");
        }

        var html = htmlFile.Data;
        
        var jsFiles = solution.Files
            .Where(f => f.Name.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
            .Where(f => !f.Name.Equals("virtual-fetch.js", StringComparison.OrdinalIgnoreCase))
            .ToList();
            
        var cssFiles = solution.Files
            .Where(f => f.Name.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            .ToList();

        html = InjectVirtualFetch(html);
        html = InlineAssets(html, jsFiles, cssFiles);
        
        return html;
    }

    public void ClearRequestLog()
    {
        _requestLog.Clear();
    }

    private string InjectVirtualFetch(string html)
    {
        const string virtualFetchScript = """
            <script>
            (function() {
                const originalFetch = window.fetch;
                const pendingRequests = new Map();
                let requestId = 0;
                
                window.addEventListener('message', (event) => {
                    if (event.data && event.data.type === 'apollo_response') {
                        const pending = pendingRequests.get(event.data.id);
                        if (pending) {
                            const response = new Response(event.data.body, {
                                status: event.data.status || 200,
                                headers: new Headers(event.data.headers || { 'Content-Type': 'application/json' })
                            });
                            pending.resolve(response);
                            pendingRequests.delete(event.data.id);
                        }
                    }
                });
                
                window.fetch = function(url, options = {}) {
                    const urlStr = typeof url === 'string' ? url : url.toString();
                    
                    if (urlStr.startsWith('/api') || urlStr.startsWith('http://localhost') || urlStr.startsWith('/')) {
                        if (urlStr.startsWith('http://localhost')) {
                            url = new URL(urlStr).pathname;
                        }
                        
                        const id = ++requestId;
                        
                        return new Promise((resolve, reject) => {
                            pendingRequests.set(id, { resolve, reject });
                            
                            parent.postMessage({
                                type: 'apollo_request',
                                id: id,
                                method: options.method || 'GET',
                                url: urlStr.startsWith('http') ? new URL(urlStr).pathname : urlStr,
                                body: options.body || null,
                                headers: options.headers || {}
                            }, '*');
                            
                            setTimeout(() => {
                                if (pendingRequests.has(id)) {
                                    pendingRequests.delete(id);
                                    reject(new Error('Request timeout'));
                                }
                            }, 30000);
                        });
                    }
                    
                    return originalFetch.call(window, url, options);
                };
                
                console.log('[Apollo] Virtual network initialized');
            })();
            </script>
            """;
        
        var headCloseIndex = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
        if (headCloseIndex > 0)
        {
            return html.Insert(headCloseIndex, virtualFetchScript);
        }
        
        return virtualFetchScript + html;
    }

    private string InlineAssets(string html, List<SolutionFile> jsFiles, List<SolutionFile> cssFiles)
    {
        foreach (var cssFile in cssFiles)
        {
            var linkPattern = $"<link[^>]*href=[\"']{cssFile.Name}[\"'][^>]*>";
            var styleTag = $"<style>/* {cssFile.Name} */\n{cssFile.Data}\n</style>";
            html = System.Text.RegularExpressions.Regex.Replace(html, linkPattern, styleTag, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        foreach (var jsFile in jsFiles)
        {
            var scriptPattern = $"<script[^>]*src=[\"']{jsFile.Name}[\"'][^>]*></script>";
            var inlineScript = $"<script>/* {jsFile.Name} */\n{jsFile.Data}\n</script>";
            html = System.Text.RegularExpressions.Regex.Replace(html, scriptPattern, inlineScript, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }

        return html;
    }

    private string BuildDefaultErrorDocument(string message)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body { 
                        font-family: system-ui, sans-serif; 
                        display: flex; 
                        align-items: center; 
                        justify-content: center; 
                        height: 100vh; 
                        margin: 0;
                        background: #1a1a2e;
                        color: #eee;
                    }
                    .error { 
                        text-align: center;
                        padding: 2rem;
                    }
                    .error h2 { color: #ff6b6b; }
                </style>
            </head>
            <body>
                <div class="error">
                    <h2>⚠️ Client Preview Error</h2>
                    <p>{{message}}</p>
                </div>
            </body>
            </html>
            """;
    }

    private static HttpMethodType ParseHttpMethod(string method) => method.ToUpperInvariant() switch
    {
        "GET" => HttpMethodType.Get,
        "POST" => HttpMethodType.Post,
        "PUT" => HttpMethodType.Put,
        "DELETE" => HttpMethodType.Delete,
        "PATCH" => HttpMethodType.Patch,
        _ => HttpMethodType.Get
    };
}

