using Apollo.Hosting;
using Apollo.Hosting.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

public class WebApplicationBuilder
{
    private readonly HostingConsoleService _console;
    
    public IServiceCollection Services { get; } = new ServiceCollection();

    internal WebApplicationBuilder(HostingConsoleService console)
    {
        _console = console;
        _console.LogTrace("WebApplicationBuilder constructed");
    }

    internal HostingConsoleService Console => _console;

    public WebApplication Build()
    {
        _console.LogTrace("Building WebApplication");
        return new WebApplication(_console);
    }
}