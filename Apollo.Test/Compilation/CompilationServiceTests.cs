using System.Reflection;
using Apollo.Compilation;
using Apollo.Components.Library.SampleProjects;
using Apollo.Components.Solutions;
using Microsoft.CodeAnalysis;
using Shouldly;
using Xunit;

namespace Apollo.Test.Compilation;

public class CompilationServiceTests
{
    private CompilationService _service;
    
    public CompilationServiceTests()
    {
        _service = new();
    }

    [Fact]
    public void Can_Compile_ConsoleApp()
    {
        // Arrange
        var solution = UntitledProject.Create();
        
        // Act
        var result = _service.Compile(solution.ToContract(), GetDefaultReferences());

        // Assert
        result.Success.ShouldBeTrue();
        result.Diagnostics.ShouldBeNull();
        result.Assembly.ShouldNotBeNull();
        result.Assembly.Length.ShouldBeGreaterThan(0);
    }
    
    [Fact]
    public void Can_Execute_ConsoleApp()
    {
        // Arrange
        var solution = UntitledProject.Create();
        var bytes = _service.Compile(solution.ToContract(), GetDefaultReferences()).Assembly;
        var asm = Assembly.Load(bytes);

        List<string> logs = [];
        
        // Act
        var result = _service.Execute(asm, (log) =>
        {
            logs.Add(log);
        }, CancellationToken.None);

        // Assert
        result.Error.ShouldBeFalse();
        result.Messages.Count.ShouldBe(0);
        logs.ShouldBeEmpty();
    }
    
    private IEnumerable<MetadataReference> GetDefaultReferences()
    {
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Console").Location)

        };

        return references;
    }
}