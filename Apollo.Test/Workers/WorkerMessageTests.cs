using System.Text.Json;
using Apollo.Contracts.Analysis;
using Apollo.Contracts.Solutions;
using Shouldly;
using Xunit;

namespace Apollo.Test.Workers;

public class WorkerMessageTests
{
    [Fact(DisplayName = "Diagnostic request serialization should preserve all fields when roundtripping")]
    public void DiagnosticRequest_Serialization_PreservesAllFields()
    {
        // Arrange
        var request = new DiagnosticRequestWrapper
        {
            Uri = "Test.cs",
            Solution = new Solution 
            {
                Items = new List<SolutionItem>
                {
                    new() { Path = "Test.cs", Content = "class Test {}" }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(request);
        var deserialized = JsonSerializer.Deserialize<DiagnosticRequestWrapper>(json);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized.Uri.ShouldBe(request.Uri);
        deserialized.Solution.Items.Count.ShouldBe(1);
        deserialized.Solution.Items[0].Content.ShouldBe(request.Solution.Items[0].Content);
    }
}