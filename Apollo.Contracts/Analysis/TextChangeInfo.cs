namespace Apollo.Contracts.Analysis;

public class TextChangeInfo
{
    public int Start { get; set; }
    public int Length { get; set; }
    public string Text { get; set; } = string.Empty;

    public TextChangeInfo() { }

    public TextChangeInfo(int start, int length, string text)
    {
        Start = start;
        Length = length;
        Text = text;
    }
}

public class DocumentUpdateRequest
{
    public string Path { get; set; } = string.Empty;
    public bool IsFullContent { get; set; }
    public string? FullContent { get; set; }
    public List<TextChangeInfo>? Changes { get; set; }
}

public class SetCurrentDocumentRequest
{
    public string Path { get; set; } = string.Empty;
}

public class UserAssemblyUpdateRequest
{
    public byte[]? AssemblyBytes { get; set; }
}

