using Apollo.Contracts.Solutions;

namespace Apollo.Components.Solutions;

public interface ISolutionItem
{
    string Uri { get; set; }
    string Name { get; set; }
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset ModifiedAt { get; set; }
    
    ISolutionItem Clone();
}

public static class SolutionItemExtensions
{
    public static bool IsCSharp(this SolutionItem file) 
        => file.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
    
    public static bool IsHtml(this SolutionItem file)
        => file.Path.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
    
    public static bool IsRazor(this SolutionItem file)
        => file.Path.EndsWith(".razor", StringComparison.OrdinalIgnoreCase);
}