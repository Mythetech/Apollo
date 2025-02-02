using System.Collections.Generic;

namespace Apollo.Analysis;

public static class CommonUsingMappings
{
    private static readonly Dictionary<string, string[]> _mappings = new()
    {
        {
            "Xunit", new[]
            {
                "xunit.core.wasm",
                "xunit.assert.wasm",
                "xunit.abstractions.wasm"
            }
        },
        {
            "NUnit.Framework", new[]
            {
                "nunit.framework.wasm"
            }
        },
        {
            "Microsoft.VisualStudio.TestTools.UnitTesting", new[]
            {
                "microsoft.visualstudio.testplatform.testframework.wasm"
            }
        }
    };

    public static IEnumerable<string> GetAssemblies(string usingNamespace)
    {
        return _mappings.TryGetValue(usingNamespace, out var assemblies) 
            ? assemblies 
            : new[] { $"{usingNamespace.Split('.')[0]}.wasm" };
    }
} 