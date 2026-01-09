using System.Diagnostics;
using Apollo.Contracts.Analysis;
using Apollo.Infrastructure;
using Apollo.Infrastructure.Workers;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Tags;
using Microsoft.Extensions.Logging;

namespace Apollo.Analysis;

using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Models;
using OmniSharp.Models.SignatureHelp;
using OmniSharp.Models.v1.Completion;
using OmniSharp.Options;
using System.Text;
using System.Collections.Immutable;
using Solution = Apollo.Contracts.Solutions.Solution;

public class MonacoService
{
    private readonly IMetadataReferenceResolver _resolver;
    private readonly ILoggerProxy _workerLogger;
    private readonly RoslynProjectService _projectService;

    private RoslynProject? _legacyCompletionProject;
    private OmniSharpCompletionService? _legacyCompletionService;
    private OmniSharpSignatureHelpService? _signatureService;
    private OmniSharpQuickInfoProvider? _quickInfoProvider;

    private RazorCodeExtractor? _razorExtractor;
    private RazorSemanticTokenService? _razorSemanticTokenService;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly JsonSerializerOptions _indentedJson = new()
    {
        WriteIndented = true
    };

    public string DefaultCode { get; init; } =
        @"
using System;

class Program
{
    static void Main()
    {
        var fizzBuzz = new FizzBuzz();
        for (int i = 1; i <= 100; i++)
        {
            Console.WriteLine(fizzBuzz.GetValue(i));
        }
    }
}
";

    public MonacoService(IMetadataReferenceResolver resolver, ILoggerProxy workerLogger)
    {
        _resolver = resolver;
        _workerLogger = workerLogger;
        _projectService = new RoslynProjectService(resolver, workerLogger);
    }

    public async Task Init(string uri)
    {
        _workerLogger.Trace($"Roslyn initializing for {uri}");
        
        await _projectService.InitializeAsync();
        
        var loggerFactory = LoggerFactory.Create(_ => { });
        var formattingOptions = new FormattingOptions();

        _signatureService = new OmniSharpSignatureHelpService(_projectService.Workspace);
        _quickInfoProvider = new OmniSharpQuickInfoProvider(_projectService.Workspace, formattingOptions, loggerFactory);

        // Initialize Razor services
        _razorExtractor = new RazorCodeExtractor(_workerLogger);
        _razorSemanticTokenService = new RazorSemanticTokenService(_razorExtractor, _projectService, _workerLogger);

        _workerLogger.Trace("RoslynProjectService initialized successfully");
    }

    public void UpdateDocument(string path, string fullText)
    {
        _projectService.UpdateDocument(path, fullText);
    }

    public void ApplyTextChanges(string path, IEnumerable<TextChangeInfo> changes)
    {
        _projectService.ApplyTextChanges(path, changes);
    }

    public void SetCurrentDocument(string path)
    {
        _projectService.SetCurrentDocument(path);
    }

    public void UpdateSolution(Solution solution)
    {
        _projectService.UpdateSolution(solution);
    }

    public void SetUserAssemblyReference(byte[] assemblyBytes)
    {
        _projectService.SetUserAssemblyReference(assemblyBytes);
    }

    public async Task<byte[]> GetCompletionAsync(string code, string completionRequestString)
    {
        try
        {
            var request = JsonSerializer.Deserialize<CompletionRequest>(completionRequestString);
            if (request == null)
            {
                _workerLogger.LogError("Failed to deserialize completion request");
                return [];
            }

            var path = request.FileName;
            _workerLogger.LogTrace($"Completion request for {path} with trigger '{request.TriggerCharacter}' at {request.Line}:{request.Column}");

            _projectService.UpdateDocument(path, code);
            _projectService.SetCurrentDocument(path);

            var document = _projectService.GetDocument(path);
            if (document == null)
            {
                _workerLogger.LogError($"Document not found: {path}");
                return [];
            }

            var sourceText = await document.GetTextAsync();
            var position = sourceText.Lines.GetPosition(new LinePosition(request.Line, request.Column));

            var completionService = CompletionService.GetService(document);
            if (completionService == null)
            {
                _workerLogger.LogError("CompletionService is null");
                return [];
            }

            var completions = await completionService.GetCompletionsAsync(document, position);
            if (completions == null)
            {
                _workerLogger.LogTrace("No completions found");
                return [];
            }

            _workerLogger.LogInformation($"Found {completions.ItemsList.Count} completion items");

            var response = new CompletionResponse
            {
                IsIncomplete = false,
                Items = completions.ItemsList.Select(item => new OmniSharp.Models.v1.Completion.CompletionItem
                {
                    Label = item.DisplayText,
                    Kind = GetCompletionItemKind(item.Tags),
                    Detail = item.Properties.GetValueOrDefault("SymbolName") ?? item.DisplayText,
                    Documentation = item.Properties.GetValueOrDefault("SymbolDescription") ?? item.InlineDescription,
                    SortText = item.SortText,
                    FilterText = item.FilterText,
                    Preselect = item.Rules.MatchPriority == MatchPriority.Preselect,
                    InsertTextFormat = InsertTextFormat.PlainText,
                    TextEdit = new LinePositionSpanTextChange
                    {
                        NewText = item.DisplayText,
                        StartLine = request.Line,
                        StartColumn = request.Column,
                        EndLine = request.Line,
                        EndColumn = request.Column
                    },
                    CommitCharacters = ['.', ';', '(', ',']
                }).ToList()
            };

            var wrapper = new CompletionResponseWrapper { Payload = response };
            var payload = new ResponsePayload(wrapper, "GetCompletionAsync");

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, _jsonOptions));
        }
        catch (Exception ex)
        {
            _workerLogger.LogError($"Error getting completions: {ex.Message}");
            _workerLogger.LogTrace(ex.StackTrace ?? string.Empty);
            return [];
        }
    }

    public async Task<byte[]> GetCompletionResolveAsync(string completionResolveRequestString)
    {
        var completionResolveRequest = JsonSerializer.Deserialize<CompletionResolveRequest>(completionResolveRequestString);
        var document = _projectService.GetCurrentDocument();
        
        if (document == null || _legacyCompletionService == null)
        {
            return [];
        }
        
        var completionResponse = await _legacyCompletionService.Handle(completionResolveRequest, document);
        var payload = new ResponsePayload(completionResponse, "GetCompletionResolveAsync");

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, _jsonOptions));
    }

    public async Task<byte[]> GetSignatureHelpAsync(string code, string signatureHelpRequestString)
    {
        var signatureHelpRequest = JsonSerializer.Deserialize<SignatureHelpRequest>(signatureHelpRequestString);
        if (signatureHelpRequest == null || _signatureService == null)
        {
            return [];
        }

        var path = signatureHelpRequest.FileName;
        _projectService.UpdateDocument(path, code);

        var document = _projectService.GetDocument(path);
        if (document == null)
        {
            return [];
        }

        var signatureHelpResponse = await _signatureService.Handle(signatureHelpRequest, document);
        var payload = new ResponsePayload(signatureHelpResponse, "GetSignatureHelpAsync");
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, _jsonOptions));
    }

    public async Task<byte[]> GetQuickInfoAsync(string quickInfoRequestString)
    {
        var quickInfoRequest = JsonSerializer.Deserialize<QuickInfoRequest>(quickInfoRequestString);
        if (quickInfoRequest == null || _quickInfoProvider == null)
        {
            return [];
        }

        var document = _projectService.GetCurrentDocument();
        if (document == null)
        {
            return [];
        }

        var quickInfoResponse = await _quickInfoProvider.Handle(quickInfoRequest, document);
        var payload = new ResponsePayload(quickInfoResponse, "GetQuickInfoAsync");
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, _jsonOptions));
    }

    public async Task<byte[]> GetDiagnosticsAsync(string uri, Solution solution)
    {
        _projectService.UpdateSolution(solution);
        
        if (solution.NuGetReferences?.Count > 0)
        {
            _projectService.SetNuGetReferences(solution.NuGetReferences);
        }

        if (_projectService.IsLoadingReferences)
        {
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new ResponsePayload(new List<Contracts.Analysis.Diagnostic>(), "GetDiagnosticsAsync"),
                _jsonOptions));
        }

        var syntaxTrees = solution.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Content) && item.Path.EndsWith(".cs"))
            .Select(item =>
            {
                _workerLogger.LogTrace($"Parsing {item.Path}");
                return CSharpSyntaxTree.ParseText(
                    item.Content,
                    RoslynProject.CompilationDefaults.ParseOptions,
                    path: item.Path);
            })
            .ToList();

        var compilation = CSharpCompilation.Create(
            "Temp" + Random.Shared.Next(),
            syntaxTrees,
            options: RoslynProject.CompilationDefaults.GetCompilationOptions(solution.Type),
            references: _projectService.GetAllReferences()
        );

        var allDiagnostics = await GetAllDiagnosticsAsync(compilation, syntaxTrees);
        foreach (var diagnostic in allDiagnostics)
        {
            _workerLogger.LogTrace($"Diagnostic: {diagnostic.Message}");
        }

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
            new ResponsePayload(allDiagnostics, "GetDiagnosticsAsync"),
            _jsonOptions));
    }

    public async Task<byte[]> HandleDocumentUpdateAsync(string requestJson)
    {
        try
        {
            var request = JsonSerializer.Deserialize<DocumentUpdateRequest>(requestJson, _jsonOptions);
            if (request == null)
            {
                return CreateErrorResponse("Invalid document update request");
            }

            if (request.IsFullContent && request.FullContent != null)
            {
                _projectService.UpdateDocument(request.Path, request.FullContent);
            }
            else if (request.Changes != null && request.Changes.Count > 0)
            {
                _projectService.ApplyTextChanges(request.Path, request.Changes);
            }

            return CreateSuccessResponse("Document updated");
        }
        catch (Exception ex)
        {
            _workerLogger.LogError($"Error handling document update: {ex.Message}");
            return CreateErrorResponse(ex.Message);
        }
    }

    public async Task<byte[]> HandleSetCurrentDocumentAsync(string requestJson)
    {
        try
        {
            var request = JsonSerializer.Deserialize<SetCurrentDocumentRequest>(requestJson, _jsonOptions);
            if (request == null)
            {
                return CreateErrorResponse("Invalid set current document request");
            }

            _projectService.SetCurrentDocument(request.Path);
            return CreateSuccessResponse("Current document set");
        }
        catch (Exception ex)
        {
            _workerLogger.LogError($"Error setting current document: {ex.Message}");
            return CreateErrorResponse(ex.Message);
        }
    }

    public async Task<byte[]> HandleUserAssemblyUpdateAsync(string requestJson)
    {
        try
        {
            var request = JsonSerializer.Deserialize<UserAssemblyUpdateRequest>(requestJson, _jsonOptions);
            if (request == null)
            {
                return CreateErrorResponse("Invalid user assembly update request");
            }

            if (request.AssemblyBytes != null && request.AssemblyBytes.Length > 0)
            {
                _projectService.SetUserAssemblyReference(request.AssemblyBytes);
            }
            else
            {
                _projectService.ClearUserAssemblyReference();
            }

            return CreateSuccessResponse("User assembly reference updated");
        }
        catch (Exception ex)
        {
            _workerLogger.LogError($"Error updating user assembly: {ex.Message}");
            return CreateErrorResponse(ex.Message);
        }
    }

    public async Task<byte[]> GetSemanticTokensAsync(string requestJson)
    {
        try
        {
            var request = JsonSerializer.Deserialize<SemanticTokensRequest>(requestJson, _jsonOptions);
            if (request == null)
            {
                _workerLogger.LogError("Failed to deserialize semantic tokens request");
                return [];
            }

            _workerLogger.LogTrace($"Semantic tokens request for {request.DocumentUri}");

            // Check if this is a Razor file
            var isRazorFile = request.DocumentUri.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) ||
                              request.DocumentUri.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase);

            if (!isRazorFile)
            {
                _workerLogger.LogTrace($"Not a Razor file: {request.DocumentUri}");
                return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                    new ResponsePayload(SemanticTokensResult.Empty, "GetSemanticTokensAsync"),
                    _jsonOptions));
            }

            if (_razorSemanticTokenService == null)
            {
                _workerLogger.LogError("Razor semantic token service not initialized");
                return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                    new ResponsePayload(SemanticTokensResult.Empty, "GetSemanticTokensAsync"),
                    _jsonOptions));
            }

            if (string.IsNullOrEmpty(request.RazorContent))
            {
                _workerLogger.LogTrace("No Razor content provided");
                return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                    new ResponsePayload(SemanticTokensResult.Empty, "GetSemanticTokensAsync"),
                    _jsonOptions));
            }

            var result = await _razorSemanticTokenService.GetSemanticTokensAsync(
                request.RazorContent,
                request.DocumentUri);

            _workerLogger.LogTrace($"Returning {result.Data.Length / 5} semantic tokens");

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new ResponsePayload(result, "GetSemanticTokensAsync"),
                _jsonOptions));
        }
        catch (Exception ex)
        {
            _workerLogger.LogError($"Error getting semantic tokens: {ex.Message}");
            _workerLogger.LogTrace(ex.StackTrace ?? string.Empty);
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new ResponsePayload(SemanticTokensResult.Empty, "GetSemanticTokensAsync"),
                _jsonOptions));
        }
    }

    private byte[] CreateSuccessResponse(string message)
    {
        var response = new { Success = true, Message = message };
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response, _jsonOptions));
    }

    private byte[] CreateErrorResponse(string message)
    {
        var response = new { Success = false, Message = message };
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response, _jsonOptions));
    }

    private async Task<List<Contracts.Analysis.Diagnostic>> GetAllDiagnosticsAsync(CSharpCompilation compilation, List<SyntaxTree> syntaxTrees)
    {
        var allDiagnostics = new List<Contracts.Analysis.Diagnostic>();

        foreach (var tree in syntaxTrees)
        {
            var diagnostics = tree.GetDiagnostics();
            foreach (var diagnostic in diagnostics)
            {
                allDiagnostics.Add(CreateDiagnostic(diagnostic));
            }
        }

        using var temp = new MemoryStream();
        var result = compilation.Emit(temp);

        var semanticDiagnostics = compilation.GetDiagnostics()
            .Concat(result.Diagnostics)
            .Where(d => d.Location.IsInSource)
            .Distinct();

        foreach (var diagnostic in semanticDiagnostics)
        {
            allDiagnostics.Add(CreateDiagnostic(diagnostic));
        }

        return allDiagnostics;
    }

    private Contracts.Analysis.Diagnostic CreateDiagnostic(Microsoft.CodeAnalysis.Diagnostic diagnostic)
    {
        var lineSpan = diagnostic.Location.GetLineSpan();
        return new Contracts.Analysis.Diagnostic
        {
            FilePath = diagnostic.Location.SourceTree?.FilePath ?? string.Empty,
            StartColumn = lineSpan.Span.Start.Character,
            StartPosition = lineSpan.Span.Start.Line + 1,
            EndColumn = lineSpan.Span.End.Character,
            EndPosition = lineSpan.Span.End.Line + 1,
            Message = diagnostic.GetMessage(),
            Severity = GetSeverity(diagnostic.Severity)
        };
    }

    private static int GetSeverity(DiagnosticSeverity severity)
    {
        return severity switch
        {
            DiagnosticSeverity.Hidden => 1,
            DiagnosticSeverity.Info => 2,
            DiagnosticSeverity.Warning => 4,
            DiagnosticSeverity.Error => 8,
            _ => throw new Exception("Unknown diagnostic severity.")
        };
    }

    private static OmniSharp.Models.v1.Completion.CompletionItemKind GetCompletionItemKind(ImmutableArray<string> tags)
    {
        if (tags.Contains(WellKnownTags.Method))
            return OmniSharp.Models.v1.Completion.CompletionItemKind.Method;
        if (tags.Contains(WellKnownTags.Class))
            return OmniSharp.Models.v1.Completion.CompletionItemKind.Class;
        if (tags.Contains(WellKnownTags.Property))
            return OmniSharp.Models.v1.Completion.CompletionItemKind.Property;
        if (tags.Contains(WellKnownTags.Field))
            return OmniSharp.Models.v1.Completion.CompletionItemKind.Field;
        if (tags.Contains(WellKnownTags.Event))
            return OmniSharp.Models.v1.Completion.CompletionItemKind.Event;
        if (tags.Contains(WellKnownTags.Enum))
            return OmniSharp.Models.v1.Completion.CompletionItemKind.Enum;
        if (tags.Contains(WellKnownTags.EnumMember))
            return OmniSharp.Models.v1.Completion.CompletionItemKind.EnumMember;
        if (tags.Contains(WellKnownTags.Structure))
            return OmniSharp.Models.v1.Completion.CompletionItemKind.Struct;
        if (tags.Contains(WellKnownTags.Interface))
            return OmniSharp.Models.v1.Completion.CompletionItemKind.Interface;
        if (tags.Contains(WellKnownTags.Delegate))
            return OmniSharp.Models.v1.Completion.CompletionItemKind.Method;
        if (tags.Contains(WellKnownTags.Keyword))
            return OmniSharp.Models.v1.Completion.CompletionItemKind.Keyword;
        if (tags.Contains(WellKnownTags.Parameter))
            return OmniSharp.Models.v1.Completion.CompletionItemKind.Variable;
        if (tags.Contains(WellKnownTags.Local))
            return OmniSharp.Models.v1.Completion.CompletionItemKind.Variable;
        if (tags.Contains(WellKnownTags.Namespace))
            return OmniSharp.Models.v1.Completion.CompletionItemKind.Module;

        return OmniSharp.Models.v1.Completion.CompletionItemKind.Text;
    }
}
