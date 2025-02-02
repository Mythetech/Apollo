using System.Reflection;
using Apollo.Components.Solutions;

namespace Apollo.Components.Code;

public interface ICompiler
{
    public async Task ExecuteAsync(SolutionModel solution, CancellationToken token = default)
    {
        await BuildAsync(solution, token);
        
        await RunAsync(null, token);
    }

    Task<CompilationResult> BuildAsync(SolutionModel solution, CancellationToken cancellationToken = default);
    Task RunAsync(Assembly assembly, CancellationToken cancellationToken = default);
}