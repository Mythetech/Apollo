using System.Text.Json;
using Apollo.Analysis;
using Apollo.Contracts.Analysis;
using Apollo.Contracts.Solutions;
using Apollo.Infrastructure.Workers;
using Apollo.Test.Workers;
using Newtonsoft.Json;
using OmniSharp.Models.Diagnostics;
using Shouldly;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Apollo.Test.Analysis;

public class DiagnosticsTests
{
    private readonly TestMetadataReferenceResolver _resolver;
    private readonly ILoggerProxy _logger;

    private static JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public DiagnosticsTests()
    {
        _resolver = new TestMetadataReferenceResolver();
        _logger = new TestLoggerProxy();
    }

    [Fact(DisplayName = "GetDiagnostics should return syntax errors for single file with missing semicolon")]
    public async Task GetDiagnostics_SingleFile_ReturnsSyntaxErrors()
    {
        // Arrange
        var solution = new Solution
        {
            Name = "Test",
            Items = new List<SolutionItem>
            {
                new() { 
                    Path = "Test.cs",
                    Content = "public class Test { string Name " // Missing semicolon
                }
            }
        };

        var service = new MonacoService(_resolver, _logger);
        await service.Init("test");

        // Act
        var diagnostics = await service.GetDiagnosticsAsync("test", solution);
        var result = System.Text.Json.JsonSerializer.Deserialize<ResponsePayload>(diagnostics, _options);

        // Assert
        result.ShouldNotBeNull();
        var payloadData = JsonSerializer.Deserialize<IEnumerable<Diagnostic>>(result.Payload.ToString(), _options);
        payloadData.ShouldNotBeNull();
        payloadData.ShouldContain(d => 
            d.Message.Contains("expected") && 
            d.FilePath == "Test.cs"
        );
    }

    [Fact(DisplayName = "GetDiagnostics should return semantic errors for type mismatch across multiple files")]
    public async Task GetDiagnostics_MultipleFiles_ReturnsSemanticErrors()
    {
        // Arrange
        var solution = new Solution
        {
            Items = new List<SolutionItem>
            {
                new() {
                    Path = "Program.cs",
                    Content = "var test = new Test(); test = 5;"
                },
                new() {
                    Path = "Test.cs",
                    Content = "public class Test {}"
                }
            }
        };

        var service = new MonacoService(_resolver, _logger);
        await service.Init("test");

        // Act
        var diagnostics = await service.GetDiagnosticsAsync("test", solution);
        var result = System.Text.Json.JsonSerializer.Deserialize<ResponsePayload>(diagnostics, _options);

        // Assert
        result.ShouldNotBeNull();
        var payloadData = JsonSerializer.Deserialize<IEnumerable<Diagnostic>>(result.Payload.ToString(), _options);
        payloadData.ShouldNotBeNull();
        payloadData.ShouldContain(d => 
            d.Message.Contains("Cannot implicitly convert type 'int' to 'Test'") && 
            d.FilePath == "Program.cs"
        );
    }
}