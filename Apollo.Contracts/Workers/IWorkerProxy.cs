namespace Apollo.Contracts.Workers;

public interface IWorkerProxy
{
        Task SendMessageAsync(WorkerMessage message);
        void OnLog(Func<LogMessage, Task> callback);
        void OnError(Func<string, Task> callback);
        Task TerminateAsync();
}