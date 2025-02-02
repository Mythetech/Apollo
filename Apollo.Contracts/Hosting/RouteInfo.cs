namespace Apollo.Contracts.Hosting;

public record RouteInfo(
    string Pattern,
    HttpMethodType Method,
    string ResponseType,
    Dictionary<string, string>? Values = default); 