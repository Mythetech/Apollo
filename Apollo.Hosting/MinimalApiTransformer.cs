namespace Apollo.Hosting;
using Apollo.Hosting.Logging;
using Microsoft.AspNetCore.Builder;
using System;
using System.Threading;
using System.Threading.Tasks;

public static class MinimalApiTransformer
{
    public static string WrapMinimalApi(string code)
    {
        var (mainCode, classDefinitions) = ExtractClassDefinitions(code);
        
        return $$"""
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using System.Text.Json;
        using Microsoft.AspNetCore.Builder;
        
        {{classDefinitions}}
        
        public class Program
        {
            public static void Main(string[] args)
            {
                {{mainCode}}
            }
        }
        """;
    }

    private static (string mainCode, string classDefinitions) ExtractClassDefinitions(string code)
    {
        var lines = code.Split('\n');
        var mainLines = new List<string>();
        var classLines = new List<string>();
        var inClassDefinition = false;
        var braceCount = 0;

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            
            if (!inClassDefinition && (trimmed.StartsWith("class ") || trimmed.StartsWith("public class ") || 
                trimmed.StartsWith("record ") || trimmed.StartsWith("public record ")))
            {
                inClassDefinition = true;
                braceCount = 0;
            }

            if (inClassDefinition)
            {
                classLines.Add(line);
                braceCount += line.Count(c => c == '{') - line.Count(c => c == '}');
                
                if (braceCount <= 0 && line.Contains('}'))
                {
                    inClassDefinition = false;
                }
            }
            else
            {
                mainLines.Add(line);
            }
        }

        return (string.Join('\n', mainLines), string.Join('\n', classLines));
    }
} 