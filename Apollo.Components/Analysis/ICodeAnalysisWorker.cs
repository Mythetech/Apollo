using Apollo.Contracts.Workers;

namespace Apollo.Components.Analysis;

public interface ICodeAnalysisWorker : IWorkerProxy
{
    public Task<byte[]> GetCompletionAsync(string code, string completionRequestString);

    public Task<byte[]> GetCompletionResolveAsync(string completionResolveRequestString);

    public Task<byte[]> GetSignatureHelpAsync(string code, string signatureHelpRequestString);

    public Task<byte[]> GetQuickInfoAsync(string quickInfoRequestString);

    public Task<byte[]> GetDiagnosticsAsync(string serializedSolution);
}