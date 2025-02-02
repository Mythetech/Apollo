using Apollo.Analysis;
using Apollo.Contracts.Solutions;
using Shouldly;
using Xunit;

namespace Apollo.Test.Components.Solutions;

public class SerializationTests
{
    public SerializationTests()
    {
        
    }
    
    [Fact]
    public void SolutionPayload_Serialization_PreservesContent()
    {
        // Arrange
        var solution = new Solution 
        {
            Name = "Test",
            Items = new List<SolutionItem>
            {
                new() 
                { 
                    Path = "Test.cs",
                    Content = "public class Test {}"
                }
            }
        };
        
        var payload = new SolutionPayload
        {
            Action = "get_diagnostics",
            Solution = solution
        };

        // Act
        var json = payload.ToJson();
        var deserialized = SolutionPayload.FromJson(json);

        // Assert
        deserialized.ShouldNotBeNull();
        deserialized!.Solution.Items.Count.ShouldBe(1);
        deserialized.Solution.Items[0].Content.ShouldBe(solution.Items[0].Content);
    }
}