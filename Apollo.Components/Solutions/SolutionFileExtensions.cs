namespace Apollo.Components.Solutions;

public static class SolutionFileExtensions
{
    public static bool IsCSharp(this SolutionFile file) 
        => file.Extension.Equals(".cs", StringComparison.OrdinalIgnoreCase);
    
    public static bool IsHtml(this SolutionFile file)
        => file.Extension.Equals(".html", StringComparison.OrdinalIgnoreCase);
    
    public static bool IsRazor(this SolutionFile file)
    => file.Extension.Equals(".razor", StringComparison.OrdinalIgnoreCase);
    
    public static string GetMonacoLanguage(this SolutionFile file) 
        => file.Extension.ToLowerInvariant() switch
        {
            ".cs" => "csharp",
            ".html" => "html",
            ".htm" => "html",
            ".css" => "css",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".json" => "json",
            ".xml" => "xml",
            ".md" => "markdown",
            ".razor" => "razor",
            ".sql" => "sql",
            ".yaml" => "yaml",
            ".yml" => "yaml",
            _ => "plaintext"
        };
}

