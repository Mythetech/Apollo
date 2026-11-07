using Apollo.Client;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Apollo.Test.Client;

public class ClientRegistrationTests
{
    [Fact(DisplayName = "Every client service can be constructed with Development validation on")]
    public void ClientServices_PassDevelopmentValidation()
    {
        var services = new ServiceCollection();
        AddWebAssemblyHostServices(services);

        services.AddApolloClient();

        Should.NotThrow(() =>
        {
            using var provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
        });
    }

    private static void AddWebAssemblyHostServices(IServiceCollection services)
    {
        var hostEnvironment = Substitute.For<IWebAssemblyHostEnvironment>();
        hostEnvironment.Environment.Returns("Development");
        hostEnvironment.BaseAddress.Returns("http://localhost/");

        services.AddSingleton(hostEnvironment);
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
        services.AddSingleton(Substitute.For<IJSRuntime>());
        services.AddSingleton<NavigationManager>(new TestNavigationManager());
        services.AddLogging();
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("http://localhost/", "http://localhost/");
    }
}
