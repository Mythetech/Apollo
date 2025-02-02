namespace Apollo.Contracts.Compilation;

public class ExecutionResult
{
    public List<string> Messages { get; set; } = [];

    public bool Error = false;
    
    public TimeSpan ExecutionTime { get; set; } = TimeSpan.Zero;
    
}