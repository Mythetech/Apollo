using Apollo.Infrastructure;
using Apollo.Infrastructure.Workers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Solution = Apollo.Contracts.Solutions.Solution;
public class ReferenceAnalyzer
{
    private readonly ILoggerProxy _logger;
    private readonly IMetadataReferenceResolver _resolver;

    public ReferenceAnalyzer(ILoggerProxy logger, IMetadataReferenceResolver resolver)
    {
        _logger = logger;
        _resolver = resolver;
    }

    public async Task<IEnumerable<MetadataReference>> GetAdditionalReferencesAsync(Solution solution)
    {
        var usings = new HashSet<string>();
        var neededAssemblies = new HashSet<string>();
        
        foreach (var item in solution.Items)
        {
            try
            {
                var tree = CSharpSyntaxTree.ParseText(item.Content);
                var root = await tree.GetRootAsync();
                
                // Get using directives
                var usingDirectives = root.DescendantNodes()
                    .OfType<UsingDirectiveSyntax>()
                    .Select(u => u.Name.ToString());
                
                foreach (var directive in usingDirectives)
                {
                    usings.Add(directive.Split('.')[0]); // Get root namespace
                }
                
                // Check for attributes
                var attributes = root.DescendantNodes()
                    .OfType<AttributeSyntax>()
                    .Select(a => a.Name.ToString());
                
                // Check for Assert usage
                var identifiers = root.DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Select(i => i.Identifier.Text);
                
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing file {item.Path}: {ex.Message}");
            }
        }

        _logger.LogTrace($"Found namespaces: {string.Join(", ", usings)}");
        _logger.LogTrace($"Found needed assemblies: {string.Join(", ", neededAssemblies)}");

        // Map common namespaces to assembly names
        var assemblyNames = usings
            .Select(u => MapNamespaceToAssembly(u))
            .Concat(neededAssemblies)
            .Where(a => !string.IsNullOrEmpty(a))
            .Distinct();

        return await Task.WhenAll(assemblyNames.Select(x => _resolver.GetMetadataReferenceAsync(x)));
    }

    private string MapNamespaceToAssembly(string ns)
    {
        // Add mappings as needed
        return ns switch
        {
            "Xunit" => "xunit.core",
            _ => string.Empty
        };
    }
} 