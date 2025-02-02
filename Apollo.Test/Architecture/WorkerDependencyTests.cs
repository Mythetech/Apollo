using System.Reflection;
using Apollo.Contracts.Workers;
using MudBlazor;
using NetArchTest.Rules;
using Shouldly;
using Xunit;

namespace Apollo.Test.Architecture;

public class WorkerDependencyTests
{
    public static readonly string[] Features =
    [
        "Apollo.Analysis", "Apollo.Analysis.Worker", 
        "Apollo.Compilation", "Apollo.Compilation.Worker",
        "Apollo.Hosting", "Apollo.Hosting.Worker"
    ];
    
    [Theory(DisplayName = "Worker should only depend on its feature, infrastructure, and contracts")]
    [InlineData("Apollo.Analysis")]
    [InlineData("Apollo.Compilation")]
    [InlineData("Apollo.Hosting")]
    public void Worker_ShouldDependOnlyOn_Itself(string feature)
    {
        var workerAssembly = Assembly.Load($"{feature}.Worker");
        
        var rules = Types
            .InAssembly(workerAssembly)
            .ShouldNot()
            .HaveDependencyOn("Apollo.Components")
            .Or()
            .HaveDependencyOn("Apollo.Client")
            .Or()
            .HaveDependencyOn("Apollo.Desktop");

        foreach (var project in Features.Where(x => !x.Contains(feature)))
        {
            rules.Or()
                .HaveDependencyOn(project);
        }

        var result = rules.GetResult();
        result.IsSuccessful.ShouldBeTrue();
        
        var contractReferences = Types
            .InAssembly(workerAssembly)
            .That()
            .HaveDependencyOnAny("Apollo.Contracts", feature)
            .GetTypes();
            
            contractReferences.Count().ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact(DisplayName = "Non WebAssembly projects should not contain concrete worker implementations")]
    public void Client_ShouldHave_WorkerImplementations()
    {
        List<Assembly> asms =
        [
            Assembly.Load("Apollo.Analysis"),
            Assembly.Load("Apollo.Compilation"),
            Assembly.Load("Apollo.Hosting"),
            Assembly.Load("Apollo.Components"),
            Assembly.Load("Apollo.Infrastructure")
        ];
        
        var result = Types.InAssemblies(asms)
            .That()
            .ImplementInterface(typeof(IWorkerProxy))
            .And()
            .AreClasses()
            .GetTypes();

        result.Count().ShouldBe(0);
    }
}