namespace Apollo.Components.Code;

public interface ICodeExecutor
{
    public Task ExecuteAsync(string code, CancellationToken token);
}