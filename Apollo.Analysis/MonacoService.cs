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
    RoslynProject _completionProject;
    RoslynProject _diagnosticProject;
    OmniSharpCompletionService _completionService;
    OmniSharpSignatureHelpService _signatureService;
    OmniSharpQuickInfoProvider _quickInfoProvider;

    JsonSerializerOptions jsonOptions = new JsonSerializerOptions {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    public MonacoService(IMetadataReferenceResolver resolver, ILoggerProxy workerLogger)
    {
        _resolver = resolver;
        _workerLogger = workerLogger;
        DefaultCode =
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
    }


    public string DefaultCode { get; init; }
    


    public async Task Init(string uri)
    {
        _workerLogger.Trace($"Roslyn initializing for {uri}");
        _completionProject = new RoslynProject(uri, _resolver, _workerLogger);
        await _completionProject.Init(uri);
        _diagnosticProject = new RoslynProject(uri, _resolver, _workerLogger);
        await _diagnosticProject.Init(uri);

        var loggerFactory = LoggerFactory.Create(configure => { });
        var formattingOptions = new FormattingOptions();

        _completionService = new OmniSharpCompletionService(_completionProject.Workspace, formattingOptions, _workerLogger);
        _signatureService = new OmniSharpSignatureHelpService(_completionProject.Workspace);
        _quickInfoProvider = new OmniSharpQuickInfoProvider(_completionProject.Workspace, formattingOptions, loggerFactory);

    }

    public async Task<byte[]> GetCompletionAsync(string code, string completionRequestString)
    {
        try
        {
            var request = JsonSerializer.Deserialize<CompletionRequest>(completionRequestString);
            if (request == null)
            {
                _workerLogger.LogError("Failed to deserialize completion request");
                return Array.Empty<byte>();
            }

            _workerLogger.LogTrace($"Completion request with trigger '{request.TriggerCharacter}' at {request.Line}:{request.Column}");
            _workerLogger.LogTrace($"Looking for document with ID {_completionProject.DocumentId.Id}");
            _workerLogger.LogTrace($"Current solution has projects: {string.Join(", ", _completionProject.Workspace.CurrentSolution.Projects.Select(p => p.Name))}");
            _workerLogger.LogTrace($"Current solution has documents: {string.Join(", ", _completionProject.Workspace.CurrentSolution.Projects.SelectMany(p => p.Documents).Select(d => d.Name))}");

            var document = _completionProject.Workspace.CurrentSolution
                .GetDocument(_completionProject.DocumentId);

            if (document == null)
                throw new Exception("Document null");
            
            document = document.WithText(SourceText.From(code));
            _completionProject.Workspace.TryApplyChanges(document.Project.Solution);

            var sourceText = await document.GetTextAsync();
            var position = sourceText.Lines.GetPosition(new LinePosition(request.Line, request.Column));

            var completionService = CompletionService.GetService(document);
            var completions = await completionService.GetCompletionsAsync(
                document,
                position
            );

            if (completions == null)
            {
                _workerLogger.LogTrace("No completions found");
                return [];
            }

            _workerLogger.LogTrace($"Found {completions.ItemsList.Count} completion items");

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
                    Preselect = item.Rules.MatchPriority == Microsoft.CodeAnalysis.Completion.MatchPriority.Preselect,
                    InsertTextFormat = InsertTextFormat.PlainText,
                    TextEdit = new LinePositionSpanTextChange
                    {
                        NewText = item.DisplayText,
                        StartLine = request.Line,
                        StartColumn = request.Column,
                        EndLine = request.Line,
                        EndColumn = request.Column
                    },
                    CommitCharacters = [ '.', ';', '(', ',' ]
                }).ToList()
            };

            var wrapper = new CompletionResponseWrapper { Payload = response };
            var payload = new ResponsePayload(wrapper, "GetCompletionAsync");

            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload, jsonOptions));
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
        var document = _completionProject.Workspace.CurrentSolution.GetDocument(_completionProject.DocumentId);
        var completionResponse = await _completionService.Handle(completionResolveRequest, document);

        ResponsePayload p = new ResponsePayload(completionResponse, "GetCompletionResolveAsync");

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(p, jsonOptions));
    }

    public async Task<byte[]> GetSignatureHelpAsync(string code, string signatureHelpRequestString)
    {
        Microsoft.CodeAnalysis.Solution updatedSolution;
        var signatureHelpRequest = JsonSerializer.Deserialize<SignatureHelpRequest>(signatureHelpRequestString);
        do
        {
            updatedSolution = _completionProject.Workspace.CurrentSolution.WithDocumentText(_completionProject.DocumentId, SourceText.From(code));
        } while (!_completionProject.Workspace.TryApplyChanges(updatedSolution));

        var document = updatedSolution.GetDocument(_completionProject.DocumentId);
        var signatureHelpResponse = await _signatureService.Handle(signatureHelpRequest, document);

        ResponsePayload p = new ResponsePayload(signatureHelpResponse, "GetSignatureHelpAsync");
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(p, jsonOptions));
    }

    public async Task<byte[]> GetQuickInfoAsync(string quickInfoRequestString)
    {
        var quickInfoRequest = JsonSerializer.Deserialize<QuickInfoRequest>(quickInfoRequestString);
        
        var document = _diagnosticProject.Workspace.CurrentSolution.GetDocument(_diagnosticProject.DocumentId);
        var quickInfoResponse = await _quickInfoProvider.Handle(quickInfoRequest, document);
        
        ResponsePayload p = new ResponsePayload(quickInfoResponse, "GetQuickInfoAsync");
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(p, jsonOptions));
    }

    public async Task<byte[]> GetDiagnosticsAsync(string uri, Solution solution)
    {
        // First update the solution and wait for references to load
        await _diagnosticProject.UpdateReferences(solution);
        
        // If still loading, return empty diagnostics
        if (_diagnosticProject.IsLoadingReferences)
        {
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
                new ResponsePayload(new List<Contracts.Analysis.Diagnostic>(), "GetDiagnosticsAsync"), 
                jsonOptions));
        }

        // Create compilation immediately after update
        var syntaxTrees = solution.Items
            .Where(item => !string.IsNullOrWhiteSpace(item.Content))
            .Select(item => {
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
            references: RoslynProject.GetMetadataReferences(_diagnosticProject)
        );

        // Get diagnostics and log them for debugging
        var allDiagnostics = await GetAllDiagnosticsAsync(compilation, syntaxTrees);
        foreach (var diagnostic in allDiagnostics)
        {
            _workerLogger.LogTrace($"Diagnostic: {diagnostic.Message}");
        }

        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(
            new ResponsePayload(allDiagnostics, "GetDiagnosticsAsync"), 
            jsonOptions));
    }

    private async Task<List<Contracts.Analysis.Diagnostic>> GetAllDiagnosticsAsync(CSharpCompilation compilation, List<SyntaxTree> syntaxTrees)
    {
        var allDiagnostics = new List<Contracts.Analysis.Diagnostic>();
        
        // Get syntax diagnostics
        foreach (var tree in syntaxTrees)
        {
            var diagnostics = tree.GetDiagnostics();
            foreach (var diagnostic in diagnostics)
            {
                allDiagnostics.Add(CreateDiagnostic(diagnostic));
            }
        }

        // Get semantic diagnostics
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
            //Start = lineSpan.StartLinePosition,
            //End = lineSpan.EndLinePosition,
            EndColumn = lineSpan.Span.End.Character,
            EndPosition = lineSpan.Span.End.Line + 1,
            Message = diagnostic.GetMessage(),
            Severity = GetSeverity(diagnostic.Severity)
        };
    }

    private int GetSeverity(DiagnosticSeverity severity)
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

    private OmniSharp.Models.v1.Completion.CompletionItemKind GetCompletionItemKind(ImmutableArray<string> tags)
    {
        // Check most specific tags first
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
        
        // Default to text if no specific tag is found
        return OmniSharp.Models.v1.Completion.CompletionItemKind.Text;
    }
}