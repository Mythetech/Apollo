namespace Apollo.Components.Solutions;

public class SolutionFile : ISolutionItem, IEquatable<SolutionFile>
{
    /// <summary>
    /// Unique virtual path to represent a single code file artifact
    /// </summary>
    public string Uri  { get; set; }
    
    public string Data { get; set; }

    public string Name { get; set; } = "Untitled.cs";
    
    public string Prefix => Name.LastIndexOf('.') > 0 ? Name[..Name.LastIndexOf('.')] : Name;

    public string Extension => Name.IndexOf(".", StringComparison.Ordinal) != -1 ? Name[Name.IndexOf(".", StringComparison.Ordinal)..] : "";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
    
    public DateTimeOffset ModifiedAt { get; set; } = DateTimeOffset.Now;
        
    public ISolutionItem Clone()
    {
        return new SolutionFile
        {
            Name = Name,
            Uri = Uri,
            Data = Data,
            CreatedAt = CreatedAt,
            ModifiedAt = ModifiedAt
        };
    }

    public bool Equals(SolutionFile? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Uri == other.Uri;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((SolutionFile)obj);
    }

    public override int GetHashCode()
    {
        return Uri.GetHashCode();
    }
}