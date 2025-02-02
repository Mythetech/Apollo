using Apollo.Contracts.Hosting;
using Apollo.Hosting;
using Apollo.Hosting.Logging;
using Microsoft.AspNetCore.Builder;
using Shouldly;
using Xunit;

namespace Apollo.Test.Hosting;

public class WebApplicationTests
{
    private readonly HostingConsoleService _console;
    private readonly WebApplication _app;

    public WebApplicationTests()
    {
        _console = new HostingConsoleService(_ => { });
        _app = new WebApplication(_console);
    }

    [Fact(DisplayName = "MapGet should register route")]
    public void MapGet_ShouldRegisterRoute()
    {
        // Arrange
        const string pattern = "/test";
        const string expected = "Hello Test";

        // Act
        _app.MapGet(pattern, () => expected);
        var routes = _app.GetRoutes();

        // Assert
        routes.Count.ShouldBe(1);
        routes[0].Pattern.ShouldBe(pattern);
        routes[0].Method.ShouldBe(HttpMethodType.Get);
        routes[0].ResponseType.ShouldBe("string");
    }

    [Fact(DisplayName = "HandleRequest should execute registered route")]
    public void HandleRequest_ShouldExecuteRegisteredRoute()
    {
        // Arrange
        const string pattern = "/test";
        const string expected = "Hello Test";
        _app.MapGet(pattern, () => expected);

        // Act
        var result = _app.HandleRequest(pattern);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact(DisplayName = "HandleRequest should return 404 for unknown route")]
    public void HandleRequest_ShouldReturn404_ForUnknownRoute()
    {
        // Act
        var result = _app.HandleRequest("/unknown");

        // Assert
        result.ShouldContain("No route found");
    }

    [Fact(DisplayName = "MapGet should handle route parameters")]
    public void MapGet_ShouldHandleRouteParameters()
    {
        // Arrange
        const string pattern = "/users/{id}";
        const string expected = "User 123";
        _app.MapGet(pattern, (string id) => $"User {id}");

        // Act
        var result = _app.HandleRequest("/users/123");

        // Assert
        result.ShouldBe(expected);
    }

    [Fact(DisplayName = "MapGet should handle multiple route parameters")]
    public void MapGet_ShouldHandleMultipleRouteParameters()
    {
        // Arrange
        const string pattern = "/users/{userId}/posts/{postId}";
        const string expected = "User 123, Post 456";
        _app.MapGet(pattern, (string userId, string postId) => $"User {userId}, Post {postId}");

        // Act
        var result = _app.HandleRequest("/users/123/posts/456");

        // Assert
        result.ShouldBe(expected);
    }

    [Fact(DisplayName = "MapPost should register route")]
    public void MapPost_ShouldRegisterRoute()
    {
        // Arrange
        const string pattern = "/users";
        const string requestBody = "{\"name\":\"Test User\"}";
        const string expected = "Created User: Test User";

        // Act
        _app.MapPost(pattern, (string body) => $"Created User: {body}");
        var routes = _app.GetRoutes();

        // Assert
        routes.Count.ShouldBe(1);
        routes[0].Pattern.ShouldBe(pattern);
        routes[0].Method.ShouldBe(HttpMethodType.Post);
        routes[0].ResponseType.ShouldBe("string");
    }

    [Fact(DisplayName = "HandleRequest should execute POST route with body")]
    public void HandleRequest_ShouldExecutePostRoute()
    {
        // Arrange
        const string pattern = "/users";
        const string requestBody = "Test User";
        const string expected = "Created User: Test User";
        _app.MapPost(pattern, (string body) => $"Created User: {body}");

        // Act
        var result = _app.HandleRequest(pattern, requestBody, HttpMethodType.Post);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact(DisplayName = "HandleRequest should handle missing body for POST")]
    public void HandleRequest_ShouldHandleMissingBody()
    {
        // Arrange
        const string pattern = "/users";
        _app.MapPost(pattern, (string body) => $"Created User: {body}");

        // Act
        var result = _app.HandleRequest(pattern, method: HttpMethodType.Post);

        // Assert
        result.ShouldContain("Created User: ");
    }

    [Fact(DisplayName = "POST should work without body when handler doesn't expect one")]
    public void Post_ShouldWorkWithoutBody_WhenHandlerDoesntExpectOne()
    {
        // Arrange
        const string pattern = "/users/{userId}/activate";
        const string expected = "User 123 activated";
        _app.MapPost(pattern, (string id) => $"User {id} activated");

        // Act
        var result = _app.HandleRequest("/users/123/activate", method: HttpMethodType.Post);

        // Assert
        result.ShouldBe(expected);
    }

    [Fact(DisplayName = "POST should accept optional body")]
    public void Post_ShouldAcceptOptionalBody()
    {
        // Arrange
        const string pattern = "/users";
        _app.MapPost(pattern, (string? body) => 
            string.IsNullOrWhiteSpace(body) ? "Created user with defaults" : $"Created user: {body}");

        // Act
        var withBody = _app.HandleRequest(pattern, "Test User", HttpMethodType.Post);
        var withoutBody = _app.HandleRequest(pattern, method: HttpMethodType.Post);

        // Assert
        withBody.ShouldBe("Created user: Test User");
        withoutBody.ShouldBe("Created user with defaults");
    }

    [Fact(DisplayName = "HandleRequest should match HTTP method")]
    public void HandleRequest_ShouldMatchHttpMethod()
    {
        // Arrange
        const string pattern = "/";
        _app.MapGet(pattern, () => "GET response");
        _app.MapPost(pattern, () => "POST response");

        // Act
        var getResult = _app.HandleRequest(pattern);
        var postResult = _app.HandleRequest(pattern, method: HttpMethodType.Post);

        // Assert
        getResult.ShouldBe("GET response");
        postResult.ShouldBe("POST response");
    }

    [Fact(DisplayName = "HandleRequest should return 404 for wrong HTTP method")]
    public void HandleRequest_ShouldReturn404_ForWrongMethod()
    {
        // Arrange
        const string pattern = "/";
        _app.MapGet(pattern, () => "GET response");

        // Act
        var result = _app.HandleRequest(pattern, method: HttpMethodType.Post);

        // Assert
        result.ShouldContain("No route found");
    }

    [Fact(DisplayName = "PUT should handle route parameters and body")]
    public void Put_ShouldHandle_RouteParamsAndBody()
    {
        // Arrange
        const string pattern = "/users/{id}";
        _app.MapPut(pattern, (string id, string body) => $"Updated user {id} with {body}");

        // Act
        var result = _app.HandleRequest("/users/123", "new data", HttpMethodType.Put);

        // Assert
        result.ShouldBe("Updated user 123 with new data");
    }

    [Fact(DisplayName = "DELETE should handle route parameters")]
    public void Delete_ShouldHandle_RouteParams()
    {
        // Arrange
        const string pattern = "/users/{id}";
        _app.MapDelete(pattern, (string id) => $"Deleted user {id}");

        // Act
        var result = _app.HandleRequest("/users/123", method: HttpMethodType.Delete);

        // Assert
        result.ShouldBe("Deleted user 123");
    }

    [Fact(DisplayName = "PATCH should handle partial updates")]
    public void Patch_ShouldHandle_PartialUpdates()
    {
        // Arrange
        const string pattern = "/users/{id}";
        _app.MapPatch(pattern, (string id, string patch) => $"Patched user {id} with {patch}");

        // Act
        var result = _app.HandleRequest("/users/123", "{\"name\":\"test\"}", HttpMethodType.Patch);

        // Assert
        result.ShouldBe("Patched user 123 with {\"name\":\"test\"}");
    }

    [Theory(DisplayName = "HTTP method should match route")]
    [InlineData(HttpMethodType.Get, "GET response")]
    [InlineData(HttpMethodType.Post, "POST response")]
    [InlineData(HttpMethodType.Put, "PUT response")]
    [InlineData(HttpMethodType.Delete, "DELETE response")]
    [InlineData(HttpMethodType.Patch, "PATCH response")]
    public void HttpMethod_ShouldMatch_Route(HttpMethodType method, string expected)
    {
        // Arrange
        const string pattern = "/test";
        switch (method)
        {
            case HttpMethodType.Get:
                _app.MapGet(pattern, () => expected);
                break;
            case HttpMethodType.Post:
                _app.MapPost(pattern, () => expected);
                break;
            case HttpMethodType.Put:
                _app.MapPut(pattern, () => expected);
                break;
            case HttpMethodType.Delete:
                _app.MapDelete(pattern, () => expected);
                break;
            case HttpMethodType.Patch:
                _app.MapPatch(pattern, () => expected);
                break;
        }

        // Act
        var result = _app.HandleRequest(pattern, method: method);

        // Assert
        result.ShouldBe(expected);
    }
} 