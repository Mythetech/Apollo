using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace Apollo.Test.Architecture;

public class ContractsDependencyTests
{
    [Fact(DisplayName = "Contracts should not depend on any other Apollo projects")]
    public void Contracts_ShouldNotDependOn_OtherApolloProjects()
    {
        var result = Types
            .InAssembly(typeof(Apollo.Contracts.Solutions.Solution).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Apollo.Analysis")
            .Or()
            .HaveDependencyOn("Apollo.Analysis.Worker")
            .Or()
            .HaveDependencyOn("Apollo.Compilation")
            .Or()
            .HaveDependencyOn("Apollo.Compilation.Worker")
            .Or()
            .HaveDependencyOn("Apollo.Hosting")
            .Or()
            .HaveDependencyOn("Apollo.Hosting.Worker")
            .Or()
            .HaveDependencyOn("Apollo.Components")
            .Or()
            .HaveDependencyOn("Apollo.Infrastructure")
            .Or()
            .HaveDependencyOn("Apollo.Client")
            .Or()
            .HaveDependencyOn("Apollo.Desktop")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue();
    }
} 