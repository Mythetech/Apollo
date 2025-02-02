namespace Apollo.Infrastructure;

public static class HostAddress
{
    #if DEBUG
    public const string BaseUri = "https://localhost:7092";
    #else
    public const string BaseUri = "https://apollo-code-editor.azurewebsites.net";
    #endif
}