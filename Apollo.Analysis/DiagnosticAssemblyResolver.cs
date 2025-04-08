using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Apollo.Infrastructure;
using Apollo.Infrastructure.Workers;
using Solution = Apollo.Contracts.Solutions.Solution;
using Apollo.Analysis;

namespace Apollo.Analysis;

public class DiagnosticAssemblyResolver
{
    private readonly IMetadataReferenceResolver _resolver;
    private readonly ILoggerProxy _logger;
    private readonly Dictionary<string, HashSet<string>> _typeResolutionCache = new();

    public DiagnosticAssemblyResolver(IMetadataReferenceResolver resolver, ILoggerProxy logger)
    {
        _resolver = resolver;
        _logger = logger;
    }

    public async Task<IEnumerable<MetadataReference>> ResolveAssemblies(Microsoft.CodeAnalysis.Solution solution, IEnumerable<Microsoft.CodeAnalysis.Diagnostic> diagnostics)
    {
        var references = new HashSet<MetadataReference>();
        
        var directAssemblies = GetDirectAssemblies(solution);
        _logger.LogTrace($"Attempting to load assemblies: {string.Join(", ", directAssemblies)}");
        
        foreach (var assembly in directAssemblies)
        {
            await TryAddReference(references, assembly);
        }

        var unresolvedTypes = diagnostics
            .Where(d => d.Id.Equals("CS0246"))
            .GroupBy(d => d.Location.GetLineSpan().StartLinePosition)
            .Select(g => ExtractTypeName(g.First().GetMessage()))
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        if (unresolvedTypes.Any())
        {
            _logger.LogTrace($"Found unresolved types: {string.Join(", ", unresolvedTypes)}");
            foreach (var type in unresolvedTypes)
            {
                if (_typeResolutionCache.TryGetValue(type, out var assemblies))
                {
                    foreach (var assembly in assemblies)
                    {
                        await TryAddReference(references, assembly);
                    }
                }
            }
        }

        return references;
    }

    private async Task TryAddReference(HashSet<MetadataReference> references, string assembly)
    {
        try 
        {
            var reference = await _resolver.GetMetadataReferenceAsync(assembly);
            if (reference != null)
            {
                references.Add(reference);
                _logger.LogTrace($"Successfully loaded assembly: {assembly}");
            }
            else
            {
                _logger.LogWarning($"Failed to load assembly: {assembly}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error loading assembly {assembly}: {ex.Message}");
        }
    }

    private string ExtractTypeName(string diagnosticMessage)
    {
        const string prefix = "The type or namespace name '";
        const string suffix = "' could not be found";

        if (diagnosticMessage.StartsWith(prefix) && diagnosticMessage.EndsWith(suffix))
        {
            return diagnosticMessage[prefix.Length..^suffix.Length];
        }

        return string.Empty;
    }

    private IEnumerable<string> GetDirectAssemblies(Microsoft.CodeAnalysis.Solution solution)
    {
        var assemblies = new HashSet<string>();
        
        foreach (var project in solution.Projects)
        {
            foreach (var document in project.Documents)
            {
                var textTask = document.GetTextAsync();
                if (!textTask.Wait(TimeSpan.FromSeconds(1))) continue;
                
                var text = textTask.Result;
                var lines = text.ToString().Split('\n');
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("using ") && trimmed.EndsWith(";"))
                    {
                        var ns = trimmed["using ".Length..^1].Trim();
                        var newAssemblies = CommonUsingMappings.GetAssemblies(ns);
                        _logger.LogTrace($"For namespace {ns}, found assemblies: {string.Join(", ", newAssemblies)}");
                        assemblies.UnionWith(newAssemblies);
                    }
                }
            }
        }

        _logger.LogTrace($"Total assemblies to load: {string.Join(", ", assemblies)}");
        return assemblies;
    }
} 