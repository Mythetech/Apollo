using System.Reflection;
using Apollo.Client.Analysis;
using Apollo.Client.Code;
using Apollo.Client.Hosting;
using Apollo.Components;
using Apollo.Components.Analysis;
using Apollo.Components.Code;
using Apollo.Components.Hosting;
using Apollo.Components.Infrastructure;
using Apollo.Infrastructure.Resources;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Mythetech.Framework.Infrastructure.Environment;
using Mythetech.Framework.Infrastructure.Mcp;
using Mythetech.Framework.Infrastructure.MessageBus;
using Mythetech.Framework.Infrastructure.Plugins;
using Mythetech.Framework.WebAssembly.Environment;

namespace Apollo.Client;

public static class ClientRegistrationExtensions
{
    public static readonly Assembly[] MessageBusAssemblies =
    [
        typeof(ClientRegistrationExtensions).Assembly,
        typeof(AppState).Assembly,
        typeof(IConsumer<>).Assembly
    ];

    public static IServiceCollection AddApolloClient(this IServiceCollection services)
    {
        services.AddScoped(sp => new HttpClient
        {
            BaseAddress = new Uri(sp.GetRequiredService<IWebAssemblyHostEnvironment>().BaseAddress)
        });

        services.AddMessageBus(MessageBusAssemblies);

        // Singleton states depend on it, and its WebAssembly dependencies are singletons.
        services.AddSingleton<IRuntimeEnvironment>(sp => new WebAssemblyRuntimeEnvironment(
            sp.GetRequiredService<IWebAssemblyHostEnvironment>(),
            sp.GetRequiredService<NavigationManager>()));

        services.AddComponentsAndServices();
        services.AddMcp(); //ToDO - fix in framework, shouldn't be required

        // The Framework assembly scan registers PluginSettingsConsumer, which needs PluginState.
        services.AddSingleton<PluginState>();

        services.AddSingleton<IResourceResolver, ResourceResolver>();

        services.AddSingleton<ICompilerWorkerFactory, CompilerWorkerFactory>();
        services.AddSingleton<ICodeAnalysisWorkerFactory, CodeAnalysisWorkerFactory>();
        services.AddSingleton<IHostingWorkerFactory, HostingWorkerFactory>();

        return services;
    }
}
