using Apollo.Contracts.Hosting;

namespace Apollo.Hosting;

public record RouteRequest(HttpMethodType Method, string Route, string? Content = default, string? RequestId = default);

public record RouteResponse(string RequestId, string Body, int StatusCode = 200);