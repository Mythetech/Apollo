namespace Apollo.Components.Solutions;

public interface ISolutionItem
{
    string Uri { get; set; }
    string Name { get; set; }
    DateTimeOffset CreatedAt { get; set; }
    DateTimeOffset ModifiedAt { get; set; }
    
    ISolutionItem Clone();
}