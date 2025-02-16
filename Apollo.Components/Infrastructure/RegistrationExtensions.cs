using System.Reflection;
using Apollo.Components.Analysis;
using Apollo.Components.Code;
using Apollo.Components.Console;
using Apollo.Components.Editor;
using Apollo.Components.Hosting;
using Apollo.Components.Infrastructure.Keyboard;
using Apollo.Components.Infrastructure.Logging;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Components.Library;
using Apollo.Components.Settings;
using Apollo.Components.Solutions;
using Apollo.Components.Solutions.Services;
using Apollo.Components.Terminal;
using Apollo.Components.Terminal.CommandServices;
using Apollo.Components.Testing;
using Blazored.LocalStorage;
using KristofferStrube.Blazor.FileSystemAccess;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using IDialogService = MudBlazor.IDialogService;
using DialogService = MudBlazor.DialogService;

namespace Apollo.Components.Infrastructure;

public static class RegistrationExtensions
{
    public static IServiceCollection AddComponentsAndServices(this IServiceCollection services)
    {
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ISnackbar, SnackbarService>();
        services.AddMudServices(cfg =>
        {
            cfg.SnackbarConfiguration.ShowTransitionDuration = 250;
            cfg.SnackbarConfiguration.HideTransitionDuration = 250;
            cfg.SnackbarConfiguration.VisibleStateDuration = 3750;
            cfg.SnackbarConfiguration.BackgroundBlurred = true;
        });
        
        services.AddFluentUIComponents();
        services.AddSingleton<IHostingService, HostingService>();
        services.AddSingleton<ConsoleOutputService>();
        services.AddSingleton<TestConsoleOutputService>();
        services.AddSingleton<AppState>();
        services.AddSingleton<TabViewState>();
        services.AddSingleton<SolutionsState>();
        services.AddSingleton<LibraryState>();
        services.AddSingleton<TestingState>();
        services.AddSingleton<CompilerState>();
        services.AddSingleton<ActiveTypeState>();
        services.AddSingleton<CodeAnalysisState>();
        services.AddSingleton<CodeAnalysisConsoleService>();
        services.AddSingleton<WebHostConsoleService>();
        services.AddScoped<KeyboardService>();
        services.AddSingleton<IFileSystemAccessServiceInProcess, FileSystemAccessServiceInProcess>(); 
        services.AddSingleton<IFileSystemAccessService>(sp => (IFileSystemAccessService) sp.GetRequiredService<IFileSystemAccessServiceInProcess>());
        services.AddTransient<IGitHubService, GitHubService>();
        services.AddTransient<Base64Service>();
        services.AddScoped<AppState>();
        services.AddScoped<SettingsState>();
        
        services.AddSingleton<TerminalState>();

        services.AddTerminalCommands();

        services.AddBlazoredLocalStorageAsSingleton(config =>
        {
            config.JsonSerializerOptions.Converters.Add(new ISolutionItemConverter());
        });
        
        services.AddTransient<ISolutionSaveService, SolutionsStorageService>();

        var systemLoggingProvider = new SystemLoggerProvider();
        services.AddSingleton(systemLoggingProvider);
        services.AddLogging(logging => 
        {
            logging.ClearProviders();
            logging.AddProvider(systemLoggingProvider);
        });
        
        return services;
    }

    public static IServiceCollection AddTerminalCommands(this IServiceCollection services)
    {
        var commandTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface && 
                        typeof(ITerminalCommand).IsAssignableFrom(t));

        foreach (var commandType in commandTypes)
        {
            services.AddScoped(typeof(ITerminalCommand), commandType);
        }

        return services;
    }
}