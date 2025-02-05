using System.Text.Json;
using Apollo.Contracts.Workers;

namespace Apollo.Infrastructure.Workers;

public static class ErrorWriter
{
    public static string SerializeErrorToWorkerMessage(Exception ex)
    {
        var msg = new WorkerMessage()
        {
            Action = StandardWorkerActions.Error,
            Payload = ex.Message
        };
        
        return JsonSerializer.Serialize(msg);
    }

    public static string SerializeErrorToWorkerMessage(string error)
    {
        var msg = new WorkerMessage()
        {
            Action = StandardWorkerActions.Error,
            Payload = error
        };
        
        return JsonSerializer.Serialize(msg);
    }
}