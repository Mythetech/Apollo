using Apollo.Contracts.Compilation;
using Apollo.Contracts.Solutions;

namespace Apollo.Components.Code;

public interface ICompilerWorker
{
    ICompilerWorker OnLog(Func<CompilerLog, Task> callback);
    ICompilerWorker OnCompileCompleted(Func<CompilationReferenceResult, Task> callback);
    
    ICompilerWorker OnExecuteCompleted(Func<ExecutionResult, Task> callback);
    ICompilerWorker OnError(Func<string, Task> callback);
    Task RequestBuildAsync(Solution solution);
    Task RequestExecuteAsync(byte[] assembly);
    Task TerminateAsync();
}