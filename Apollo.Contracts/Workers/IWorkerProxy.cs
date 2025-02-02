namespace Apollo.Contracts.Workers;

public interface IWorkerProxy
{
        void OnLog(Func<LogMessage, Task> callback);
        void OnError(Func<string, Task> callback);
        Task TerminateAsync();
}