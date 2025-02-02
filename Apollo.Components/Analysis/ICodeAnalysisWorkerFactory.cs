namespace Apollo.Components.Analysis;

public interface ICodeAnalysisWorkerFactory
{
    public Task<ICodeAnalysisWorker> CreateAsync();
}