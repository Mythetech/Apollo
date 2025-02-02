namespace Apollo.Contracts.Analysis;

public class CompletionRequestWrapper
{
    public string Code { get; set; }
    
    public string Request { get; set; }

    public CompletionRequestWrapper(string code, string request)
    {
        Code = code;
        Request = request;
    }
}