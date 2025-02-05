using Apollo.Contracts.Compilation;
using Apollo.Contracts.Solutions;
using Apollo.Contracts.Workers;

namespace Apollo.Components.Code;

public interface ICompilerWorker : IWorkerProxy
{
    ICompilerWorker OnCompileCompleted(Func<CompilationReferenceResult, Task> callback);
    
    ICompilerWorker OnExecuteCompleted(Func<ExecutionResult, Task> callback);
    Task RequestBuildAsync(Solution solution);
    Task RequestExecuteAsync(byte[] assembly);
}