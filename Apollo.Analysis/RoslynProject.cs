using System.Collections.Immutable;
using Apollo.Contracts.Solutions;
using Apollo.Infrastructure;
using Apollo.Infrastructure.Workers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;
using OmniSharp.Models.v1.Completion;
using OmniSharp.Options;
using OmniSharp.Roslyn.CSharp.Services;
using Solution = Apollo.Contracts.Solutions.Solution;

namespace Apollo.Analysis;

public class RoslynProject
{
    private readonly IMetadataReferenceResolver _resolver;
    private readonly ILoggerProxy _logger;
    private readonly List<MetadataReference> _metadataReferences = new();
    private readonly AdhocWorkspace _workspace;
    private readonly OmniSharpCompletionService _completionService;
    private bool _isLoadingReferences;

    private static readonly ImmutableArray<string> _defaultNamespaces = ImmutableArray.Create(
        "System",
        "System.IO",
        "System.Net",
        "System.Linq",
        "System.Text",
        "System.Text.RegularExpressions",
        "System.Collections.Generic",
        "System.Threading",
        "System.Threading.Tasks"
    );

    private static readonly string[] _coreAssemblies =
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
    ];

    public static IEnumerable<MetadataReference> GetMetadataReferences(RoslynProject project) => project._metadataReferences;

    public static class CompilationDefaults 
    {
        public static readonly CSharpParseOptions ParseOptions = new(
            LanguageVersion.Latest,
            DocumentationMode.Parse,
            SourceCodeKind.Regular,
            preprocessorSymbols: ImmutableArray<string>.Empty);

        public static CSharpCompilationOptions GetCompilationOptions(ProjectType projectType = ProjectType.Console) => new(
            projectType switch {
                ProjectType.WebApi => OutputKind.DynamicallyLinkedLibrary,
                _ => OutputKind.ConsoleApplication
            },
            optimizationLevel: OptimizationLevel.Debug,
            allowUnsafe: true,
            concurrentBuild: true,
            metadataImportOptions: MetadataImportOptions.Public);
    }

    public RoslynProject(string uri, IMetadataReferenceResolver resolver, ILoggerProxy logger)
    {
        _logger = logger;
        _resolver = resolver;
        _workspace = new AdhocWorkspace();
        
        var projectId = ProjectId.CreateNewId();
        var projectInfo = ProjectInfo.Create(
            projectId,
            VersionStamp.Create(),
            "Project",
            "Project",
            LanguageNames.CSharp)
            .WithCompilationOptions(CompilationDefaults.GetCompilationOptions());

        var project = _workspace.AddProject(projectInfo);
        var document = _workspace.AddDocument(projectId, uri, SourceText.From(string.Empty));
        DocumentId = document.Id;

        _logger.LogTrace($"Created document with ID {DocumentId.Id}");
        _completionService = new OmniSharpCompletionService(_workspace, new FormattingOptions(), logger);
    }

    private async Task LoadInitialReferences()
    {
        foreach (var assembly in _coreAssemblies)
        {
            var reference = await _resolver.GetMetadataReferenceAsync(assembly);
            if (reference != null)
            {
                _metadataReferences.Add(reference);
                _logger.LogTrace($"Added reference to {assembly}");
            }
            else
            {
                _logger.LogWarning($"Failed to load assembly: {assembly}");
            }
        }

        var xunitAssemblies = new[] { "xunit.core.wasm", "xunit.assert.wasm", "xunit.abstractions.wasm" };
        foreach (var assembly in xunitAssemblies)
        {
            var reference = await _resolver.GetMetadataReferenceAsync(assembly);
            if (reference != null)
            {
                _metadataReferences.Add(reference);
                _logger.LogTrace($"Added reference to {assembly}");
            }
        }

        var project = _workspace.CurrentSolution.GetProject(DocumentId.ProjectId);
        if (project != null)
        {
            var newProject = project.WithMetadataReferences(_metadataReferences);
            var newSolution = newProject.Solution;
            _workspace.TryApplyChanges(newSolution);
        }
    }

    public async Task UpdateReferences(Contracts.Solutions.Solution apolloSolution)
    {
        _isLoadingReferences = true;
        try
        {
            var project = _workspace.CurrentSolution.GetProject(DocumentId.ProjectId);
            if (project == null) return;

            project = project.WithCompilationOptions(CompilationDefaults.GetCompilationOptions(apolloSolution.Type));

            var compilation = await project.GetCompilationAsync();
            if (compilation == null) return;

            var diagnostics = compilation.GetDiagnostics();
            var requiredAssemblies = DetectRequiredAssemblies(apolloSolution);

            var resolver = new AssemblyResolver(_resolver, _logger);
            var additionalReferences = (await resolver.ResolveAssemblies(_workspace.CurrentSolution, diagnostics)).ToList();

            foreach (string s in requiredAssemblies)
            {
                _logger.LogTrace($"Determined assembly {s} is required");
                //additionalReferences.Add(await _resolver.GetMetadataReferenceAsync(s));
            }
            foreach (var reference in additionalReferences)
            {
                if (!_metadataReferences.Contains(reference))
                {
                    _metadataReferences.Add(reference);
                }
            }

            var newProject = project
                .WithMetadataReferences(_metadataReferences)
                .WithCompilationOptions(CompilationDefaults.GetCompilationOptions(apolloSolution.Type));

            var newSolution = newProject.Solution;
            if (!_workspace.TryApplyChanges(newSolution))
            {
                _logger.LogWarning("Failed to apply solution changes");
            }

            _completionService.UpdateWorkspace(_workspace);
        }
        finally
        {
            _isLoadingReferences = false;
        }
    }

    public AdhocWorkspace Workspace => _workspace;
    public Document UseOnlyOnceDocument => _workspace.CurrentSolution.GetDocument(DocumentId) 
        ?? throw new InvalidOperationException("Document not found");
    public DocumentId DocumentId { get; private set; }
    public bool IsLoadingReferences => _isLoadingReferences;

    private static HashSet<string> DetectRequiredAssemblies(Solution solution)
    {
        var assemblies = new HashSet<string>();
        
        foreach (var item in solution.Items)
        {
            var content = item.Content;
            if (string.IsNullOrEmpty(content)) continue;

            var lines = content.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
               
                if (trimmed.StartsWith("using ") && trimmed.EndsWith(";"))
                {
                    var ns = trimmed["using ".Length..^1].Trim();
                    var assemblyName = ns.Split('.')[0];
                    assemblies.Add($"{assemblyName}.wasm");
                }
            }
        }
        
        return assemblies;
    }

    public async Task<IEnumerable<Microsoft.CodeAnalysis.Completion.CompletionItem>> GetCompletions(int position, string text)
    {
        var document = Workspace.CurrentSolution.GetDocument(DocumentId)
            .WithText(SourceText.From(text));

        // Use OmniSharp completion service instead of direct Roslyn completion
        return await _completionService.GetCompletionsAsync(document, position);
    }

    private bool IsInsideAttributeArguments(SyntaxToken token)
    {
        var parent = token.Parent;
        while (parent != null)
        {
            // Check if we're inside attribute argument list
            if (parent.Kind() == SyntaxKind.AttributeArgumentList &&
                parent.ToString().Contains("Fact"))
            {
                return true;
            }
            parent = parent.Parent;
        }
        return false;
    }

    public async Task Init(string text)
    {
        // Load references first
        await LoadInitialReferences();

        var document = _workspace.CurrentSolution
            .GetDocument(DocumentId)
            ?.WithText(SourceText.From(text));

        if (document != null)
        {
            var solution = document.Project.Solution;
            if (_workspace.TryApplyChanges(solution))
            {
                await UpdateReferences(new Contracts.Solutions.Solution 
                { 
                    Items = [new SolutionItem { Content = text } ]
                });
            }
        }
    }
}