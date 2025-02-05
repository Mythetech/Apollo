using Apollo.Hosting;
using Apollo.Hosting.Logging;
using System.Text.Json;
using Apollo.Contracts.Hosting;
using Apollo.Contracts.Workers;
using Apollo.Infrastructure.Workers;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

public class WebApplication
{
    private readonly Dictionary<RoutePattern, Delegate> _routes = new();
    private readonly List<RouteInfo> _routeInfos = new();
    private readonly HostingConsoleService _console;
    private bool _started;

    public static WebApplication Current;

    public static CancellationTokenSource CurrentCancellationToken = new();
    
    public static WebApplicationBuilder CreateBuilder(string[] args = null)
    {
        var console = CompilationContext.GetConsole() ?? throw new InvalidOperationException("Console service not initialized");
        console.LogTrace("Creating WebApplicationBuilder");
        try
        {
            return new WebApplicationBuilder(console);
        }
        catch (Exception ex)
        {
            console.LogError($"Error creating builder: {ex}");
            throw;
        }
    }

    internal WebApplication(HostingConsoleService console)
    {
        try
        {
            _console = console;
            Current = this;  // Set current instance immediately
            _console.LogTrace("WebApplication object created");
        }
        catch (Exception ex)
        {
            console.LogError($"Error in WebApplication constructor: {ex}");
            throw;
        }
    }

    public WebApplication MapGet(string pattern, Delegate handler)
    {
        var routePattern = new RoutePattern(pattern, HttpMethodType.Get);
        _routes[routePattern] = handler;
        _routeInfos.Add(new RouteInfo(pattern, HttpMethodType.Get, "string"));
        return this;
    }

    public WebApplication MapPost(string pattern, Delegate handler)
    {
        var routePattern = new RoutePattern(pattern, HttpMethodType.Post);
        
        bool hasRouteParameters = pattern.Contains("{") && pattern.Contains("}");
        
        if (!hasRouteParameters && handler is Func<string, string> bodyHandler)
        {
            _routes[routePattern] = new Func<string>(() => 
            {
                var requestBody = CompilationContext.GetRequestBody();
                return bodyHandler(requestBody ?? "");
            });
        }
        else if (!hasRouteParameters && handler is Func<string?, string> optionalBodyHandler)
        {
            _routes[routePattern] = new Func<string>(() => 
            {
                var requestBody = CompilationContext.GetRequestBody();
                return optionalBodyHandler(requestBody);
            });
        }
        else 
        {
            _routes[routePattern] = handler;
        }
        
        _routeInfos.Add(new RouteInfo(pattern, HttpMethodType.Post, "string"));
        return this;
    }

    public WebApplication MapPost(string pattern, Func<string> handler)
    {
        _routes[new RoutePattern(pattern, HttpMethodType.Post)] = handler;
        _routeInfos.Add(new RouteInfo(pattern, HttpMethodType.Post, "string"));
        return this;
    }

    public WebApplication MapPut(string pattern, Delegate handler)
    {
        var routePattern = new RoutePattern(pattern, HttpMethodType.Put);
        bool hasRouteParameters = pattern.Contains("{") && pattern.Contains("}");
        
        if (hasRouteParameters && handler.Method.GetParameters().Length > 1)
        {
            _routes[routePattern] = new Func<string, string>((id) => 
            {
                var requestBody = CompilationContext.GetRequestBody();
                return handler.DynamicInvoke(id, requestBody ?? "").ToString();
            });
        }
        else if (!hasRouteParameters && handler is Func<string, string> bodyHandler)
        {
            _routes[routePattern] = new Func<string>(() => 
            {
                var requestBody = CompilationContext.GetRequestBody();
                return bodyHandler(requestBody ?? "");
            });
        }
        else 
        {
            _routes[routePattern] = handler;
        }
        
        _routeInfos.Add(new RouteInfo(pattern, HttpMethodType.Put, "string"));
        return this;
    }

    public WebApplication MapDelete(string pattern, Delegate handler)
    {
        var routePattern = new RoutePattern(pattern, HttpMethodType.Delete);
        _routes[routePattern] = handler;
        _routeInfos.Add(new RouteInfo(pattern, HttpMethodType.Delete, "string"));
        return this;
    }

    public WebApplication MapPatch(string pattern, Delegate handler)
    {
        var routePattern = new RoutePattern(pattern, HttpMethodType.Patch);
        bool hasRouteParameters = pattern.Contains("{") && pattern.Contains("}");
        
        if (hasRouteParameters && handler.Method.GetParameters().Length > 1)
        {
            _routes[routePattern] = new Func<string, string>((id) => 
            {
                var requestBody = CompilationContext.GetRequestBody();
                return handler.DynamicInvoke(id, requestBody ?? "").ToString();
            });
        }
        else if (!hasRouteParameters && handler is Func<string, string> bodyHandler)
        {
            _routes[routePattern] = new Func<string>(() => 
            {
                var requestBody = CompilationContext.GetRequestBody();
                return bodyHandler(requestBody ?? "");
            });
        }
        else 
        {
            _routes[routePattern] = handler;
        }
        
        _routeInfos.Add(new RouteInfo(pattern, HttpMethodType.Patch, "string"));
        return this;
    }

    public IReadOnlyList<RouteInfo> GetRoutes() => _routeInfos.AsReadOnly();

    public async Task RunAsync(CancellationToken token = default)
    {
        if (_started) return;
        _started = true;

        _console.LogInfo("Application started. Press Ctrl+C to shut down.");
        
        try
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(5000, token);
                _console.LogTrace("Web application heartbeat..."); 
            }
        }
        catch (OperationCanceledException)
        {
            _console.LogInfo("Application is shutting down...");
        }
        finally
        {
            _console.LogTrace("Run loop completed");
        }
    }

    public void Run()
    {
        _console.LogTrace("Run called");
        
        var routes = GetRoutes();
        var msg = new WorkerMessage 
        {
            Action = WorkerActions.RoutesDiscovered,
            Payload = JsonSerializer.Serialize(routes)
        };
        
        CompilationContext.SendMessage(msg);
        _console.LogInfo($"Discovered routes: {JsonSerializer.Serialize(routes)}");
        
        _ = RunAsync(CurrentCancellationToken.Token);
        _console.LogTrace("RunAsync started");
    }

    private string GetTimestamp() => $"[{DateTimeOffset.UtcNow:HH:mm:ss.fff}]";

    public string HandleRequest(string path, string? body = null, HttpMethodType method = HttpMethodType.Get)
    {
        _console.LogDebug($"{GetTimestamp()} Handling {method} request to {path}");
        
        CompilationContext.SetRequestBody(body);
        
        foreach (var route in _routes)
        {
            if (route.Key.TryMatch(path, method, out var parameters))
            {
                try 
                {
                    var startTime = DateTimeOffset.UtcNow;
                    var result = parameters.Count > 0 
                        ? route.Value.DynamicInvoke(parameters.Values.ToArray())
                        : route.Value.DynamicInvoke();
                    var duration = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                    
                    _console.LogInfo($"{GetTimestamp()} {method} {path} => {result} ({duration:F2}ms)");
                    return result?.ToString() ?? "";
                }
                catch (System.Reflection.TargetInvocationException tie) 
                {
                    if (tie.InnerException is InvalidOperationException ioe && 
                        ioe.Message.Contains("No request body provided"))
                    {
                        return ioe.Message;
                    }
                    
                    var error = $"Error executing route {path}: {tie.InnerException?.Message ?? tie.Message}";
                    _console.LogError($"{GetTimestamp()} {error}");
                    return error;
                }
                catch (Exception ex)
                {
                    var error = $"Error executing route {path}: {ex.Message}";
                    _console.LogError($"{GetTimestamp()} {error}");
                    return error;
                }
                finally
                {
                    CompilationContext.SetRequestBody(null);
                }
            }
        }
        
        var notFound = $"No route found for {method} '{path}'";
        _console.LogWarning($"{GetTimestamp()} {notFound}");
        return notFound;
    }

    private Dictionary<string, string> ExtractRouteParameters(string pattern, string path)
    {
        var parameters = new Dictionary<string, string>();
        var patternParts = pattern.Split('/');
        var pathParts = path.Split('/');

        if (patternParts.Length != pathParts.Length)
            return parameters;

        for (var i = 0; i < patternParts.Length; i++)
        {
            if (patternParts[i].StartsWith("{") && patternParts[i].EndsWith("}"))
            {
                var paramName = patternParts[i].Trim('{', '}');
                parameters[paramName] = pathParts[i];
            }
        }

        return parameters;
    }
} 