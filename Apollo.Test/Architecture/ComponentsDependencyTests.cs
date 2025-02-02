using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace Apollo.Test.Architecture;

public class ComponentsDependencyTests
{
    [Fact(DisplayName = "Components should not depend on Client or Desktop")]
    public void Components_ShouldNotDependOn_ClientOrDesktop()
    {
        var result = Types
            .InAssembly(typeof(Apollo.Components.Editor.ApolloCodeEditor).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Apollo.Client")
            .Or()
            .HaveDependencyOn("Apollo.Desktop")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }
    
    [Fact(DisplayName = "Components should not depend on Encapsulated Feature Projects")]
    public void Components_ShouldNotDependOn_FeatureProjects()
    {
        var result = Types
            .InAssembly(typeof(Apollo.Components.Editor.ApolloCodeEditor).Assembly)
            .ShouldNot()
            .HaveDependencyOnAll("Apollo.Analysis", "Apollo.Compilation", "Apollo.Hosting")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }
    
    [Fact(DisplayName = "Components should not depend on Workers")]
    public void Components_ShouldNotDependOn_Workers()
    {
        var result = Types
            .InAssembly(typeof(Apollo.Components.Editor.ApolloCodeEditor).Assembly)
            .ShouldNot()
            .HaveDependencyOnAll("Apollo.Analysis.Worker", "Apollo.Compilation.Worker", "Apollo.Hosting.Worker")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }

    [Fact(DisplayName = "Components should depend on Contracts")]
    public void Components_ShouldBeAllowedToDependOn_Contracts()
    {
        var result = Types
            .InAssembly(typeof(Apollo.Components.Editor.ApolloCodeEditor).Assembly)
            .That()
            .HaveDependencyOn("Apollo.Contracts")
            .GetTypes();

        result.Count().ShouldBeGreaterThanOrEqualTo(1);
    }
} 