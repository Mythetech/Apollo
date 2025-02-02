using Apollo.Contracts.Hosting;

namespace Apollo.Hosting;

public record RouteRequest(HttpMethodType Method, string Route, string? Content = default);