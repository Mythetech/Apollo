using Apollo.Components.Library.SampleProjects;
using Apollo.Components.Solutions.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Apollo.Test.Components.Solutions.Services;

public class Base64ServiceTests
{
    private readonly Base64Service _service;
    private readonly ILogger<Base64Service> _logger;

    public Base64ServiceTests()
    {
        _logger = Substitute.For<ILogger<Base64Service>>();
        _service = new Base64Service(_logger);
    }

    [Fact(DisplayName = "EncodeSolution should create valid encoded string")]
    public void EncodeSolution_ShouldCreateValidBase64String()
    {
        // Arrange
        var solution = UntitledProject.Create();

        // Act
        var base64 = _service.EncodeSolution(solution);

        // Assert
        base64.ShouldNotBeNullOrEmpty();

        Should.NotThrow(() => _service.DecodeSolution(base64));
    }

    [Fact(DisplayName = "DecodeSolution should restore solution from base64")]
    public void DecodeSolution_ShouldRestoreSolution()
    {
        // Arrange
        var original = FizzBuzzProject.Create();
        var base64 = _service.EncodeSolution(original);

        // Act
        var decoded = _service.DecodeSolution(base64);

        // Assert
        decoded.ShouldNotBeNull();
        decoded.Name.ShouldBe(original.Name);
        decoded.Items.Count.ShouldBe(original.Items.Count);
        decoded.Items[0].Uri.ShouldBe(original.Items[0].Uri);
        decoded.Files.Count.ShouldBe(original.Files.Count);
        decoded.Files[0].Data.ShouldBe(original.Files[0].Data);
    }

    [Fact(DisplayName = "DecodeSolution should handle invalid base64")]
    public void DecodeSolution_ShouldHandleInvalidBase64()
    {
        // Act
        var result = _service.DecodeSolution("invalid-base64");

        // Assert
        result.ShouldBeNull();
    }
} 