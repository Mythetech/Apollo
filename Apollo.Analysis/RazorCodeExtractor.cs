using System.Collections.Immutable;
using Apollo.Infrastructure.Workers;
using Microsoft.AspNetCore.Razor.Language;

namespace Apollo.Analysis;

/// <summary>
/// Extracts C# code from Razor files using the Razor compiler.
/// Provides source mappings to translate positions between generated C# and original Razor.
/// </summary>
public class RazorCodeExtractor
{
    private readonly ILoggerProxy _logger;

    public RazorCodeExtractor(ILoggerProxy logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse a Razor file and return the extraction result containing generated C# and source mappings.
    /// </summary>
    public RazorExtractionResult Extract(string razorContent, string filePath)
    {
        try
        {
            // Create a minimal file system for the Razor engine
            var fileSystem = new VirtualRazorProjectFileSystem();

            // Determine file kind based on extension
            var fileKind = filePath.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase)
                ? FileKinds.Legacy
                : FileKinds.Component;

            // Create the Razor project engine with component configuration
            var projectEngine = RazorProjectEngine.Create(
                RazorConfiguration.Default,
                fileSystem,
                builder =>
                {
                    // Configure for Blazor component compilation
                    builder.SetRootNamespace("Apollo.Generated");
                });

            // Create a source document from the Razor content
            var sourceDocument = RazorSourceDocument.Create(razorContent, filePath);

            // Create and process the code document
            var codeDocument = projectEngine.Process(
                sourceDocument,
                fileKind,
                ImmutableArray<RazorSourceDocument>.Empty,
                tagHelpers: null);

            // Get the generated C# document
            var csharpDocument = codeDocument.GetCSharpDocument();
            if (csharpDocument == null)
            {
                _logger.LogTrace($"Failed to generate C# from Razor file: {filePath}");
                return RazorExtractionResult.Empty;
            }

            // Log any diagnostics from Razor compilation
            foreach (var diagnostic in csharpDocument.Diagnostics)
            {
                _logger.LogTrace($"Razor diagnostic in {filePath}: {diagnostic.GetMessage()}");
            }

            // Get syntax tree
            var syntaxTree = codeDocument.GetSyntaxTree();

            return new RazorExtractionResult
            {
                GeneratedCode = csharpDocument.GeneratedCode,
                SourceMappings = csharpDocument.SourceMappings.ToList(),
                SyntaxTree = syntaxTree
            };
        }
        catch (Exception ex)
        {
            _logger.LogTrace($"Error extracting C# from Razor file {filePath}: {ex.Message}");
            return RazorExtractionResult.Empty;
        }
    }
}

/// <summary>
/// Result of Razor extraction containing generated C# code and source mappings.
/// </summary>
public class RazorExtractionResult
{
    /// <summary>
    /// The generated C# code that can be analyzed by Roslyn.
    /// </summary>
    public string GeneratedCode { get; init; } = "";

    /// <summary>
    /// Source mappings that map spans in generated C# back to original Razor positions.
    /// </summary>
    public List<SourceMapping> SourceMappings { get; init; } = [];

    /// <summary>
    /// The Razor syntax tree for component detection.
    /// </summary>
    public RazorSyntaxTree? SyntaxTree { get; init; }

    /// <summary>
    /// An empty extraction result.
    /// </summary>
    public static RazorExtractionResult Empty => new();

    /// <summary>
    /// Whether this result has valid generated code.
    /// </summary>
    public bool IsEmpty => string.IsNullOrEmpty(GeneratedCode);
}

/// <summary>
/// A virtual file system implementation for the Razor project engine.
/// Since we're processing in-memory content, we don't need actual file system access.
/// </summary>
internal class VirtualRazorProjectFileSystem : RazorProjectFileSystem
{
    public override IEnumerable<RazorProjectItem> EnumerateItems(string basePath)
    {
        return Enumerable.Empty<RazorProjectItem>();
    }

    public override RazorProjectItem GetItem(string path)
    {
        return new NotFoundProjectItem(string.Empty, path, FileKinds.Component);
    }

    public override RazorProjectItem GetItem(string path, string? fileKind)
    {
        return new NotFoundProjectItem(string.Empty, path, fileKind ?? FileKinds.Component);
    }
}

/// <summary>
/// Represents a project item that was not found.
/// </summary>
internal class NotFoundProjectItem : RazorProjectItem
{
    public NotFoundProjectItem(string basePath, string path, string fileKind)
    {
        BasePath = basePath;
        FilePath = path;
        FileKind = fileKind;
    }

    public override string BasePath { get; }
    public override string FilePath { get; }
    public override string FileKind { get; }
    public override bool Exists => false;
    public override string PhysicalPath => FilePath;

    public override Stream Read()
    {
        throw new InvalidOperationException("Item does not exist");
    }
}
