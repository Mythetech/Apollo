using Apollo.Contracts.Workers;

namespace Apollo.Components.Analysis;

public interface ICodeAnalysisWorker : IWorkerProxy
{
    Task<byte[]> GetCompletionAsync(string code, string completionRequestString);

    Task<byte[]> GetCompletionResolveAsync(string completionResolveRequestString);

    Task<byte[]> GetSignatureHelpAsync(string code, string signatureHelpRequestString);

    Task<byte[]> GetQuickInfoAsync(string quickInfoRequestString);

    Task<byte[]> GetDiagnosticsAsync(string serializedSolution);
    
    Task<byte[]> UpdateDocumentAsync(string documentUpdateRequest);
    
    Task<byte[]> SetCurrentDocumentAsync(string setCurrentDocumentRequest);
    
    Task<byte[]> UpdateUserAssemblyAsync(string userAssemblyUpdateRequest);
}
