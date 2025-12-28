using Apollo.Contracts.Hosting;
using Apollo.Contracts.Workers;

namespace Apollo.Components.Hosting;

public interface IHostingWorker : IWorkerProxy
{
    public Task RunAsync(string code);

    public Task<string> SendAsync(HttpMethodType method, string path, string? body = default);
    
    public Task StopAsync();

    public void OnRoutesDiscovered(Func<IReadOnlyList<RouteInfo>, Task> callback);
}