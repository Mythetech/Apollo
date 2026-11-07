using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Apollo.Client;
using Mythetech.Framework.Infrastructure.MessageBus;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddApolloClient();

var app = builder.Build();

app.Services.UseMessageBus(ClientRegistrationExtensions.MessageBusAssemblies);

await app.RunAsync();
