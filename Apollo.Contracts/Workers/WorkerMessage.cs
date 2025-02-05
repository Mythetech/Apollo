using System.Text.Json;

namespace Apollo.Contracts.Workers;

public class WorkerMessage
{
    
        public string Action { get; set; } = string.Empty; 
        public string Payload { get; set; } = string.Empty;

        public string ToSerialized()
        {
                return JsonSerializer.Serialize(this);
        }
}