using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Apollo.Client;
using Apollo.Client.Analysis;
using Apollo.Client.Code;
using Apollo.Client.Debugging;
using Apollo.Client.Hosting;
using Apollo.Client.Infrastructure;
using Apollo.Components.Analysis;
using Apollo.Components.Code;
using Apollo.Components.Debugging;
using Apollo.Components.Hosting;
using Apollo.Components.Infrastructure;
using Apollo.Components.Infrastructure.Environment;
using Apollo.Components.Infrastructure.MessageBus;
using Apollo.Infrastructure.Resources;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddMessageBus(typeof(Program).Assembly, typeof(IConsumer<>).Assembly);

builder.Services.AddSingleton<IRuntimeEnvironment, WebAssemblyRuntimeEnvironment>();

builder.Services.AddComponentsAndServices();

builder.Services.AddSingleton<IResourceResolver, ResourceResolver>();

builder.Services.AddSingleton<ICompilerWorkerFactory, CompilerWorkerFactory>();
builder.Services.AddSingleton<ICodeAnalysisWorkerFactory, CodeAnalysisWorkerFactory>();
builder.Services.AddSingleton<IHostingWorkerFactory, HostingWorkerFactory>();
builder.Services.AddSingleton<IDebuggerWorkerFactory, DebuggerWorkerFactory>();

var app = builder.Build();

app.Services.UseMessageBus(typeof(Program).Assembly, typeof(IConsumer<>).Assembly);

await app.RunAsync();