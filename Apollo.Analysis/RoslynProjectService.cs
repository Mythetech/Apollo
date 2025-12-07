using System.Collections.Concurrent;
using Apollo.Contracts.Analysis;
using Apollo.Contracts.Solutions;
using Apollo.Infrastructure;
using Apollo.Infrastructure.Workers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Models.v1.Completion;
using OmniSharp.Options;
using Solution = Apollo.Contracts.Solutions.Solution;

namespace Apollo.Analysis;

public class RoslynProjectService
{
    private readonly IMetadataReferenceResolver _resolver;
    private readonly ILoggerProxy _logger;
    private readonly List<MetadataReference> _systemReferences = [];
    private readonly ConcurrentDictionary<string, DocumentId> _documentIds = new();
    private readonly AdhocWorkspace _workspace;
    private readonly OmniSharpCompletionService _completionService;
    private readonly OmniSharpSignatureHelpService _signatureService;
    private readonly OmniSharpQuickInfoProvider _quickInfoProvider;
    
    private ProjectId _projectId;
    private string? _currentDocumentPath;
    private MetadataReference? _userAssemblyReference;
    private readonly List<MetadataReference> _nugetReferences = [];
    private bool _isInitialized;
    private bool _isLoadingReferences;

    private static readonly string[] CoreAssemblies =
    [
        "System.Runtime.wasm",
        "System.Private.CoreLib.wasm",
        "System.Console.wasm",
        "System.Collections.wasm",
        "System.Linq.wasm",
        "System.Threading.wasm",
        "System.Threading.Tasks.wasm",
        "System.Text.RegularExpressions.wasm",
        "System.ObjectModel.wasm",
        "System.ComponentModel.wasm",
        "System.ComponentModel.Primitives.wasm",
        "System.Linq.Expressions.wasm",
        "System.Runtime.CompilerServices.Unsafe.wasm",
        "System.Runtime.InteropServices.wasm",
        "Apollo.Hosting.wasm",
        "xunit.core.wasm",
        "xunit.assert.wasm",
        "xunit.abstractions.wasm"
    ];

    public AdhocWorkspace Workspace => _workspace;
    public string? CurrentDocumentPath => _currentDocumentPath;
    public bool IsInitialized => _isInitialized;
    public bool IsLoadingReferences => _isLoadingReferences;

    public RoslynProjectService(IMetadataReferenceResolver resolver, ILoggerProxy logger)
    {
        _resolver = resolver;
        _logger = logger;
        _workspace = new AdhocWorkspace();
        
        var formattingOptions = new FormattingOptions();
        var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(_ => { });
        
        _completionService = new OmniSharpCompletionService(_workspace, formattingOptions, _logger);
        _signatureService = new OmniSharpSignatureHelpService(_workspace);
        _quickInfoProvider = new OmniSharpQuickInfoProvider(_workspace, formattingOptions, loggerFactory);
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        
        _logger.LogTrace("Initializing RoslynProjectService...");
        
        await LoadSystemReferencesAsync();
        CreateProject();
        
        _isInitialized = true;
        _logger.LogTrace("RoslynProjectService initialized");
    }

    private async Task LoadSystemReferencesAsync()
    {
        _isLoadingReferences = true;
        try
        {
            foreach (var assembly in CoreAssemblies)
            {
                var reference = await _resolver.GetMetadataReferenceAsync(assembly);
                if (reference != null)
                {
                    _systemReferences.Add(reference);
                    _logger.LogTrace($"Loaded reference: {assembly}");
                }
                else
                {
                    _logger.LogWarning($"Failed to load assembly: {assembly}");
                }
            }
        }
        finally
        {
            _isLoadingReferences = false;
        }
    }

    private void CreateProject()
    {
        _projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            _projectId,
            VersionStamp.Create(),
            "UserProject",
            "UserProject",
            LanguageNames.CSharp)
            .WithCompilationOptions(RoslynProject.CompilationDefaults.GetCompilationOptions())
            .WithParseOptions(RoslynProject.CompilationDefaults.ParseOptions)
            .WithMetadataReferences(_systemReferences);

        _workspace.AddProject(projectInfo);
        _logger.LogTrace($"Created project with ID {_projectId.Id}");
    }

    public void SetCurrentDocument(string path)
    {
        _currentDocumentPath = path;
        _logger.LogTrace($"Current document set to: {path}");
    }

    public void UpdateDocument(string path, string fullText)
    {
        if (!_isInitialized)
        {
            _logger.LogWarning("RoslynProjectService not initialized");
            return;
        }

        var sourceText = SourceText.From(fullText);

        if (_documentIds.TryGetValue(path, out var existingDocumentId))
        {
            var solution = _workspace.CurrentSolution.WithDocumentText(existingDocumentId, sourceText);
            _workspace.TryApplyChanges(solution);
            _logger.LogTrace($"Updated document: {path}");
        }
        else
        {
            var documentId = DocumentId.CreateNewId(_projectId);
            var documentInfo = DocumentInfo.Create(
                documentId,
                path,
                loader: TextLoader.From(TextAndVersion.Create(sourceText, VersionStamp.Create())),
                filePath: path);

            var solution = _workspace.CurrentSolution.AddDocument(documentInfo);
            _workspace.TryApplyChanges(solution);
            _documentIds[path] = documentId;
            _logger.LogTrace($"Added new document: {path} with ID {documentId.Id}");
        }

        _completionService.UpdateWorkspace(_workspace);
    }

    public void ApplyTextChanges(string path, IEnumerable<TextChangeInfo> changes)
    {
        if (!_isInitialized || !_documentIds.TryGetValue(path, out var documentId))
        {
            _logger.LogWarning($"Cannot apply changes - document not found: {path}");
            return;
        }

        var document = _workspace.CurrentSolution.GetDocument(documentId);
        if (document == null)
        {
            _logger.LogWarning($"Document not found in workspace: {path}");
            return;
        }

        var sourceText = document.GetTextAsync().Result;
        var textChanges = changes.Select(c => new TextChange(
            new TextSpan(c.Start, c.Length),
            c.Text
        )).ToArray();

        var newText = sourceText.WithChanges(textChanges);
        var newSolution = _workspace.CurrentSolution.WithDocumentText(documentId, newText);
        _workspace.TryApplyChanges(newSolution);
        
        _logger.LogTrace($"Applied {textChanges.Length} text changes to {path}");
    }

    public void RemoveDocument(string path)
    {
        if (!_documentIds.TryRemove(path, out var documentId)) return;

        var solution = _workspace.CurrentSolution.RemoveDocument(documentId);
        _workspace.TryApplyChanges(solution);
        _logger.LogTrace($"Removed document: {path}");
    }

    public void UpdateSolution(Solution apolloSolution)
    {
        if (!_isInitialized) return;

        var existingPaths = _documentIds.Keys.ToHashSet();
        var newPaths = apolloSolution.Items.Select(i => i.Path).ToHashSet();
        
        foreach (var pathToRemove in existingPaths.Except(newPaths))
        {
            RemoveDocument(pathToRemove);
        }
        
        foreach (var item in apolloSolution.Items)
        {
            if (!string.IsNullOrWhiteSpace(item.Content))
            {
                UpdateDocument(item.Path, item.Content);
            }
        }
        
        UpdateProjectType(apolloSolution.Type);
    }

    public void UpdateProjectType(ProjectType projectType)
    {
        var project = _workspace.CurrentSolution.GetProject(_projectId);
        if (project == null) return;

        var newOptions = RoslynProject.CompilationDefaults.GetCompilationOptions(projectType);
        var newSolution = _workspace.CurrentSolution.WithProjectCompilationOptions(_projectId, newOptions);
        _workspace.TryApplyChanges(newSolution);
    }

    public void SetUserAssemblyReference(byte[] assemblyBytes)
    {
        if (assemblyBytes == null || assemblyBytes.Length == 0)
        {
            ClearUserAssemblyReference();
            return;
        }

        try
        {
            _userAssemblyReference = MetadataReference.CreateFromImage(assemblyBytes);
            UpdateProjectReferences();
            _logger.LogTrace("User assembly reference updated");
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Failed to create user assembly reference: {ex.Message}");
        }
    }

    public void ClearUserAssemblyReference()
    {
        _userAssemblyReference = null;
        UpdateProjectReferences();
        _logger.LogTrace("User assembly reference cleared");
    }

    private void UpdateProjectReferences()
    {
        var project = _workspace.CurrentSolution.GetProject(_projectId);
        if (project == null) return;

        var references = new List<MetadataReference>(_systemReferences);
        if (_userAssemblyReference != null)
        {
            references.Add(_userAssemblyReference);
        }

        var newSolution = _workspace.CurrentSolution.WithProjectMetadataReferences(_projectId, references);
        _workspace.TryApplyChanges(newSolution);
        _completionService.UpdateWorkspace(_workspace);
    }

    public async Task<CompletionResponse?> GetCompletionsAsync(string path, int line, int column, CompletionRequest request)
    {
        if (!_documentIds.TryGetValue(path, out var documentId))
        {
            _logger.LogWarning($"Document not found for completions: {path}");
            return null;
        }

        var document = _workspace.CurrentSolution.GetDocument(documentId);
        if (document == null) return null;

        return await _completionService.Handle(request, document);
    }

    public async Task<IEnumerable<Microsoft.CodeAnalysis.Completion.CompletionItem>> GetCompletionItemsAsync(string path, int position)
    {
        if (!_documentIds.TryGetValue(path, out var documentId))
        {
            _logger.LogWarning($"Document not found: {path}");
            return [];
        }

        var document = _workspace.CurrentSolution.GetDocument(documentId);
        if (document == null) return [];

        var completionService = CompletionService.GetService(document);
        if (completionService == null) return [];

        var completions = await completionService.GetCompletionsAsync(document, position);
        return completions?.ItemsList ?? [];
    }

    public Document? GetCurrentDocument()
    {
        if (_currentDocumentPath == null || !_documentIds.TryGetValue(_currentDocumentPath, out var documentId))
            return null;

        return _workspace.CurrentSolution.GetDocument(documentId);
    }

    public Document? GetDocument(string path)
    {
        if (!_documentIds.TryGetValue(path, out var documentId))
            return null;

        return _workspace.CurrentSolution.GetDocument(documentId);
    }

    public async Task<CSharpCompilation?> GetCompilationAsync()
    {
        var project = _workspace.CurrentSolution.GetProject(_projectId);
        if (project == null) return null;

        return await project.GetCompilationAsync() as CSharpCompilation;
    }

    public IEnumerable<MetadataReference> GetSystemReferencesOnly()
    {
        return _systemReferences;
    }

    public IEnumerable<MetadataReference> GetAllReferences()
    {
        var references = new List<MetadataReference>(_systemReferences);
        references.AddRange(_nugetReferences);
        if (_userAssemblyReference != null)
        {
            references.Add(_userAssemblyReference);
        }
        return references;
    }

    public void SetNuGetReferences(IEnumerable<NuGetReference> nugetRefs)
    {
        _nugetReferences.Clear();
        foreach (var nugetRef in nugetRefs)
        {
            if (nugetRef.AssemblyData?.Length > 0)
            {
                try
                {
                    var reference = MetadataReference.CreateFromImage(nugetRef.AssemblyData);
                    _nugetReferences.Add(reference);
                    _logger.LogTrace($"Added NuGet reference for analysis: {nugetRef.AssemblyName}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"Failed to add NuGet reference {nugetRef.AssemblyName}: {ex.Message}");
                }
            }
        }
        
        UpdateNugetProjectReferences();
    }

    private void UpdateNugetProjectReferences()
    {
        var currentSolution = _workspace.CurrentSolution;
        var project = currentSolution.GetProject(_projectId);
        if (project == null) return;

        var allReferences = GetAllReferences().ToList();
        var newSolution = currentSolution.WithProjectMetadataReferences(_projectId, allReferences);
        _workspace.TryApplyChanges(newSolution);
        _logger.LogTrace($"Updated project with {allReferences.Count} total references");
    }
}

