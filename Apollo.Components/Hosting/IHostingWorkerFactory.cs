namespace Apollo.Components.Hosting;

public interface IHostingWorkerFactory
{
    public Task<IHostingWorker> CreateAsync(CancellationToken cancellationToken);
}