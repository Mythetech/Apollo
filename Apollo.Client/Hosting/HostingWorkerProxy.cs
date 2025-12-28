using System.Collections.Concurrent;
using System.Text.Json;
using Apollo.Components.Console;
using Apollo.Components.Hosting;
using Apollo.Contracts.Hosting;
using Apollo.Contracts.Workers;
using Apollo.Hosting;
using Apollo.Infrastructure.Workers;
using KristofferStrube.Blazor.DOM;
using KristofferStrube.Blazor.WebWorkers;
using KristofferStrube.Blazor.Window;
using Microsoft.JSInterop;

namespace Apollo.Client.Hosting;

public class HostingWorkerProxy : IHostingWorker
{
    private readonly SlimWorker _worker;
    private readonly Dictionary<string, Delegate> _callbacks = new();
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pendingRequests = new();
    private readonly IJSRuntime _jsRuntime;
    private readonly WebHostConsoleService _console;
    private int _requestCounter;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    protected internal HostingWorkerProxy(SlimWorker worker, IJSRuntime jsRuntime, WebHostConsoleService console)
    {
        _worker = worker;
        _jsRuntime = jsRuntime;
        _console = console;
    }
    
    internal async Task InitializeMessageListener()
    {
        var eventListener = await EventListener<MessageEvent>.CreateAsync(_jsRuntime, async e =>
        {
            object? data = await e.Data.GetValueAsync();
            if (data is string json)
            {
                var message = JsonSerializer.Deserialize<WorkerMessage>(json);
                if (message == null)
                {
                    _console.AddWarning("Failed to deserialize worker message");
                    return;
                }

                _console.AddDebug($"Processing message action: {message.Action}");

                switch (message.Action)
                {
                    case StandardWorkerActions.Log:
                        if (_callbacks.TryGetValue(StandardWorkerActions.Log, out var logCallback) &&
                            logCallback is Func<LogMessage, Task> typedLogCallback)
                        {
                            var log = JsonSerializer.Deserialize<LogMessage>(message.Payload);
                            if (log != null)
                            {
                                await typedLogCallback.Invoke(log);
                            }
                        }

                        break;

                    case StandardWorkerActions.Error:
                        if (_callbacks.TryGetValue(StandardWorkerActions.Error, out var errorCallback) &&
                            errorCallback is Func<string, Task> typedErrorCallback)
                        {
                            var error = message.Payload;
                            if (error != null)
                            {
                                await typedErrorCallback.Invoke(error);
                            }
                        }

                        break;
                    case WorkerActions.RoutesDiscovered:
                        _console.AddDebug("Processing routes_discovered message");
                        if (_callbacks.TryGetValue(WorkerActions.RoutesDiscovered, out var routesCallback))
                        {
                            _console.AddDebug($"Found routes callback: {routesCallback?.GetType().Name}");
                            if (routesCallback is Func<IReadOnlyList<RouteInfo>, Task> typedRoutesCallback)
                            {
                                _console.AddDebug($"Received routes message: {message.Payload}");
                                var routes = JsonSerializer.Deserialize<List<RouteInfo>>(message.Payload);
                                if (routes != null)
                                {
                                    _console.AddDebug($"Deserialized {routes.Count} routes");
                                    await typedRoutesCallback.Invoke(routes);
                                }
                                else
                                {
                                    _console.AddWarning("Failed to deserialize routes");
                                }
                            }
                            else
                            {
                                _console.AddWarning($"Routes callback was wrong type: {routesCallback?.GetType().Name}");
                            }
                        }
                        else
                        {
                            _console.AddWarning("No routes callback registered");
                        }
                        break;
                    case WorkerActions.RouteResponse:
                        HandleRouteResponse(message.Payload);
                        break;
                    case "":
                        break;

                    default:
                        _console.AddWarning($"Unknown event: {message.Action}");
                        _console.AddDebug(JsonSerializer.Serialize(message.Payload));
                        break;
                }
            }

        });

        await _worker.AddOnMessageEventListenerAsync(eventListener);
    }

    private void HandleRouteResponse(string payload)
    {
        try
        {
            var response = JsonSerializer.Deserialize<RouteResponse>(payload, SerializerOptions);
            if (response == null)
            {
                _console.AddWarning($"Failed to deserialize route response: {payload}");
                return;
            }

            _console.AddInfo($"Response received for {response.RequestId}: {response.StatusCode}");

            if (_pendingRequests.TryRemove(response.RequestId, out var tcs))
            {
                tcs.TrySetResult(response.Body);
            }
            else
            {
                _console.AddWarning($"No pending request found for {response.RequestId}");
            }
        }
        catch (Exception ex)
        {
            _console.AddError($"Error handling route response: {ex.Message}");
        }
    }

    public void OnLog(Func<LogMessage, Task> callback)
    {
        _callbacks[StandardWorkerActions.Log] = callback;
    }

    public void OnError(Func<string, Task> callback)
    {
         _callbacks[StandardWorkerActions.Error] = callback;
    }

    public void OnRoutesDiscovered(Func<IReadOnlyList<RouteInfo>, Task> callback)
    {
        _callbacks[WorkerActions.RoutesDiscovered] = callback;
    }

    public async Task TerminateAsync()
    {
        await _worker.DisposeAsync();
    }

    public async Task RunAsync(string code)
    {
        var msg = new WorkerMessage()
        {
            Action = WorkerActions.Run,
            Payload = code
        };
        
        _console.AddDebug($"Requesting solution run");
        await _worker.PostMessageAsync(msg.ToSerialized());
    }

    public async Task<string> SendAsync(HttpMethodType method, string path, string? body = default)
    {
        var requestId = $"req_{Interlocked.Increment(ref _requestCounter)}_{DateTimeOffset.UtcNow.Ticks}";
        var tcs = new TaskCompletionSource<string>();
        
        _pendingRequests[requestId] = tcs;
        
        var request = new RouteRequest(method, path, body, requestId);
        var msg = new WorkerMessage()
        {
            Action = WorkerActions.Send,
            Payload = JsonSerializer.Serialize(request, SerializerOptions)
        };
        
        await _worker.PostMessageAsync(msg.ToSerialized());

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        cts.Token.Register(() =>
        {
            if (_pendingRequests.TryRemove(requestId, out var pendingTcs))
            {
                pendingTcs.TrySetException(new TimeoutException($"Request {requestId} timed out"));
            }
        });

        return await tcs.Task;
    }

    public async Task StopAsync()
    {
        var msg = new WorkerMessage()
        {
            Action = WorkerActions.Stop,
            Payload = ""
        };
        await _worker.PostMessageAsync(msg.ToSerialized());
    }
}
