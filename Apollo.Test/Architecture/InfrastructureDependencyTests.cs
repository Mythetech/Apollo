using Apollo.Contracts.Workers;
using Apollo.Infrastructure;
using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace Apollo.Test.Architecture;

public class InfrastructureDependencyTests
{
    [Fact(DisplayName = "Infrastructure should only depend on Contracts")]
    public void Infrastructure_ShouldOnlyDependOn_Contracts()
    {
        var result = Types
            .InAssembly(typeof(IMetadataReferenceResolver).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Apollo.Analysis")
            .Or()
            .HaveDependencyOn("Apollo.Analysis.Worker")
            .Or()
            .HaveDependencyOn("Apollo.Components")
            .Or()
            .HaveDependencyOn("Apollo.Client")
            .Or()
            .HaveDependencyOn("Apollo.Desktop")
            .Or()
            .HaveDependencyOn("Apollo.Compilation")
            .Or()
            .HaveDependencyOn("Apollo.Compilation.Worker")
            .Or()
            .HaveDependencyOn("Apollo.Hosting")
            .Or()
            .HaveDependencyOn("Apollo.Hosting.Worker")
            .GetResult();
        
        var contractReferences = Types
            .InAssembly(typeof(Apollo.Components.Editor.ApolloCodeEditor).Assembly)
            .That()
            .HaveDependencyOn("Apollo.Contracts")
            .GetTypes();

        result.IsSuccessful.ShouldBeTrue();
        contractReferences.Count().ShouldBeGreaterThan(1);
    }
} 