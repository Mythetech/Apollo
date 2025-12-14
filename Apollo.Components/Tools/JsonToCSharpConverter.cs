using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Apollo.Components.Tools;

public static class JsonToCSharpConverter
{
    public static string Convert(string json, string rootClassName = "Root")
    {
        if (string.IsNullOrWhiteSpace(json))
            return "";

        try
        {
            using var doc = JsonDocument.Parse(json);
            var classDefinitions = new List<(string Name, string Definition)>();
            var processedTypes = new HashSet<string>();

            ProcessElement(doc.RootElement, rootClassName, classDefinitions, processedTypes);

            var sb = new StringBuilder();
            foreach (var (name, definition) in classDefinitions)
            {
                sb.AppendLine(definition);
                sb.AppendLine();
            }

            return sb.ToString().TrimEnd();
        }
        catch (JsonException ex)
        {
            return $"// Error parsing JSON: {ex.Message}";
        }
    }

    private static void ProcessElement(JsonElement element, string className, List<(string Name, string Definition)> classDefinitions, HashSet<string> processedTypes)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var sanitizedClassName = SanitizeClassName(className);
            
            if (!processedTypes.Contains(sanitizedClassName))
            {
                processedTypes.Add(sanitizedClassName);
                
                var sb = new StringBuilder();
                sb.AppendLine($"public class {sanitizedClassName}");
                sb.AppendLine("{");

                var properties = new List<(string Name, JsonElement Value)>();

                foreach (var prop in element.EnumerateObject())
                {
                    properties.Add((prop.Name, prop.Value));
                }

                foreach (var (propName, propValue) in properties)
                {
                    var propType = GetCSharpType(propValue, propName, sanitizedClassName, processedTypes);
                    var sanitizedPropName = SanitizePropertyName(propName);
                    sb.AppendLine($"    public {propType} {sanitizedPropName} {{ get; set; }}");
                }

                sb.AppendLine("}");
                
                classDefinitions.Add((sanitizedClassName, sb.ToString()));
            }

            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Value.ValueKind == JsonValueKind.Object)
                {
                    var nestedClassName = SanitizeClassName(prop.Name);
                    ProcessElement(prop.Value, nestedClassName, classDefinitions, processedTypes);
                }
                else if (prop.Value.ValueKind == JsonValueKind.Array && prop.Value.GetArrayLength() > 0)
                {
                    var firstItem = prop.Value[0];
                    if (firstItem.ValueKind == JsonValueKind.Object)
                    {
                        var nestedClassName = SanitizeClassName(prop.Name);
                        ProcessElement(firstItem, nestedClassName, classDefinitions, processedTypes);
                    }
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array && element.GetArrayLength() > 0)
        {
            var firstItem = element[0];
            if (firstItem.ValueKind == JsonValueKind.Object)
            {
                ProcessElement(firstItem, className, classDefinitions, processedTypes);
            }
        }
    }

    private static string GetCSharpType(JsonElement element, string propertyName, string parentClassName, HashSet<string> processedTypes)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => element.TryGetInt64(out _) ? "long" : "double",
            JsonValueKind.True or JsonValueKind.False => "bool",
            JsonValueKind.Null => "string?",
            JsonValueKind.Object => SanitizeClassName(propertyName),
            JsonValueKind.Array => element.GetArrayLength() > 0
                ? GetArrayType(element[0], propertyName, parentClassName, processedTypes)
                : "List<object>",
            _ => "object"
        };
    }

    private static string GetArrayType(JsonElement firstElement, string propertyName, string parentClassName, HashSet<string> processedTypes)
    {
        if (firstElement.ValueKind == JsonValueKind.Object)
        {
            var itemClassName = SanitizeClassName(propertyName);
            return $"List<{itemClassName}>";
        }

        var elementType = GetCSharpType(firstElement, propertyName, parentClassName, processedTypes);
        return $"List<{elementType}>";
    }

    private static string SanitizeClassName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Root";

        var sanitized = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");
        if (string.IsNullOrWhiteSpace(sanitized) || char.IsDigit(sanitized[0]))
            sanitized = "Class" + sanitized;

        return char.ToUpperInvariant(sanitized[0]) + sanitized.Substring(1);
    }

    private static string SanitizePropertyName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Property";

        var sanitized = Regex.Replace(name, @"[^a-zA-Z0-9_]", "");
        if (string.IsNullOrWhiteSpace(sanitized) || char.IsDigit(sanitized[0]))
            sanitized = "Property" + sanitized;

        return char.ToUpperInvariant(sanitized[0]) + sanitized.Substring(1);
    }
}
