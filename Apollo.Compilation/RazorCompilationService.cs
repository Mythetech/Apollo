using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Apollo.Contracts.Compilation;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Solution = Apollo.Contracts.Solutions.Solution;

namespace Apollo.Compilation;

/// <summary>
/// Compiles Razor component files (.razor) to assemblies.
/// </summary>
public class RazorCompilationService
{
    /// <summary>
    /// Compiles a solution containing Razor files to an assembly.
    /// </summary>
    public CompilationReferenceResult Compile(Solution solution, IEnumerable<MetadataReference> references)
    {
        var stopwatch = Stopwatch.StartNew();
        var syntaxTrees = new List<SyntaxTree>();
        var diagnostics = new List<string>();

        diagnostics.Add($"Starting Razor compilation for {solution.Name} with {solution.Items.Count} items");

        var importFiles = solution.Items
            .Where(item => item.Path.EndsWith("_Imports.razor", StringComparison.OrdinalIgnoreCase))
            .ToList();

        diagnostics.Add($"Found {importFiles.Count} _Imports.razor file(s)");

        var importSourceDocuments = importFiles
            .Select(import => RazorSourceDocument.Create(import.Content, import.Path))
            .ToImmutableArray();

        foreach (var item in solution.Items)
        {
            if (item.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase) &&
                !item.Path.EndsWith("_Imports.razor", StringComparison.OrdinalIgnoreCase))
            {
                diagnostics.Add($"Processing Razor file: {item.Path}");

                var (generatedCode, genDiagnostics) = GenerateComponentCode(item.Path, item.Content, importSourceDocuments);
                diagnostics.AddRange(genDiagnostics);

                if (!string.IsNullOrEmpty(generatedCode))
                {
                    generatedCode = EnsureEssentialUsings(generatedCode);
                    syntaxTrees.Add(CSharpSyntaxTree.ParseText(generatedCode, path: item.Path + ".g.cs"));
                    diagnostics.Add($"Generated {generatedCode.Length} chars of C# code for {Path.GetFileName(item.Path)}");
                }
                else
                {
                    diagnostics.Add($"Failed to generate code for {item.Path}");
                }
            }
            else if (item.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(item.Content, path: item.Path));
            }
        }

        if (syntaxTrees.Count == 0)
        {
            stopwatch.Stop();
            diagnostics.Add("No compilable files found");
            return new CompilationReferenceResult(false, null, diagnostics, stopwatch.Elapsed);
        }

        diagnostics.Add($"Compiling {syntaxTrees.Count} syntax trees with {references.Count()} references");

        var compilation = CSharpCompilation.Create(
            solution.Name,
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, concurrentBuild: true)
        );

        using var memoryStream = new MemoryStream();
        var emitResult = compilation.Emit(memoryStream);

        stopwatch.Stop();

        if (!emitResult.Success)
        {
            var emitDiagnostics = emitResult.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => $"{d.Location}: {d.GetMessage()}")
                .ToList();
            diagnostics.AddRange(emitDiagnostics);
            diagnostics.Add($"Compilation failed with {emitDiagnostics.Count} errors");
            return new CompilationReferenceResult(false, null, diagnostics, stopwatch.Elapsed);
        }

        memoryStream.Seek(0, SeekOrigin.Begin);
        var assemblyBytes = memoryStream.ToArray();

        diagnostics.Add($"Compilation successful, assembly size: {assemblyBytes.Length} bytes");

        return new CompilationReferenceResult(true, assemblyBytes, diagnostics, stopwatch.Elapsed);
    }

    /// <summary>
    /// Generates C# code from a Razor component file.
    /// </summary>
    private (string code, List<string> diagnostics) GenerateComponentCode(string filePath, string razorContent, ImmutableArray<RazorSourceDocument> importSources)
    {
        var diagnostics = new List<string>();

        try
        {
            var fileSystem = new VirtualRazorProjectFileSystem();

            var projectEngine = RazorProjectEngine.Create(
                RazorConfiguration.Default,
                fileSystem,
                builder =>
                {
                    builder.SetRootNamespace("UserComponents");
                });

            var sourceDocument = RazorSourceDocument.Create(razorContent, filePath);

            var codeDocument = projectEngine.Process(
                sourceDocument,
                FileKinds.Component,
                importSources,
                tagHelpers: null);

            var csharpDocument = codeDocument.GetCSharpDocument();

            if (csharpDocument == null)
            {
                diagnostics.Add("Razor compiler returned null C# document, using fallback");
                return (GenerateSimpleComponentCode(filePath, razorContent), diagnostics);
            }

            foreach (var diag in csharpDocument.Diagnostics)
            {
                diagnostics.Add($"Razor: {diag.GetMessage()}");
            }

            var generatedCode = csharpDocument.GeneratedCode;

            if (string.IsNullOrEmpty(generatedCode))
            {
                diagnostics.Add("Razor compiler returned empty code, using fallback");
                return (GenerateSimpleComponentCode(filePath, razorContent), diagnostics);
            }

            diagnostics.Add("Razor compiler succeeded");
            return (generatedCode, diagnostics);
        }
        catch (Exception ex)
        {
            diagnostics.Add($"Razor compiler exception: {ex.Message}, using fallback");
            return (GenerateSimpleComponentCode(filePath, razorContent), diagnostics);
        }
    }

    /// <summary>
    /// Fallback simple code generation when Razor compiler fails.
    /// Generates a minimal but functional component.
    /// </summary>
    private string GenerateSimpleComponentCode(string filePath, string razorContent)
    {
        var componentName = Path.GetFileNameWithoutExtension(filePath);

        var codeBlockMatch = Regex.Match(
            razorContent,
            @"@code\s*\{([\s\S]*)\}\s*$",
            RegexOptions.Multiline);

        var codeBlock = codeBlockMatch.Success ? codeBlockMatch.Groups[1].Value.Trim() : "";

        var usings = new List<string>();
        var usingMatches = Regex.Matches(razorContent, @"@using\s+([\w\.]+)");
        foreach (Match match in usingMatches)
        {
            usings.Add($"using {match.Groups[1].Value};");
        }

        var usingsBlock = string.Join("\n", usings);

        return $$"""
// <auto-generated/>
#pragma warning disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Rendering;
{{usingsBlock}}

namespace UserComponents
{
    public partial class {{componentName}} : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder __builder)
        {
            __builder.OpenElement(0, "div");
            __builder.AddAttribute(1, "class", "component-preview");
            __builder.AddContent(2, "{{componentName}} Component");
            __builder.CloseElement();
        }

{{codeBlock}}
    }
}
#pragma warning restore
""";
    }

    /// <summary>
    /// Ensures essential using statements are present in the generated C# code.
    /// The Razor compiler may not emit all necessary usings for async/Task support.
    /// </summary>
    private static string EnsureEssentialUsings(string generatedCode)
    {
        const string taskUsing = "using System.Threading.Tasks;";

        if (generatedCode.Contains(taskUsing))
            return generatedCode;

        var insertIndex = 0;
        var pragmaIndex = generatedCode.IndexOf("#pragma warning disable", StringComparison.Ordinal);
        if (pragmaIndex >= 0)
        {
            var lineEnd = generatedCode.IndexOf('\n', pragmaIndex);
            if (lineEnd >= 0)
                insertIndex = lineEnd + 1;
        }

        return generatedCode.Insert(insertIndex, taskUsing + "\n");
    }
}

/// <summary>
/// A virtual file system implementation for the Razor project engine.
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
