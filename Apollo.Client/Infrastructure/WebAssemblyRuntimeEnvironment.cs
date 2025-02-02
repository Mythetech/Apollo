using System.Reflection;
using Apollo.Components.Infrastructure.Environment;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace Apollo.Client.Infrastructure;

public class WebAssemblyRuntimeEnvironment : IRuntimeEnvironment
{
    private readonly IWebAssemblyHostEnvironment _host;

    public WebAssemblyRuntimeEnvironment(IWebAssemblyHostEnvironment host)
    {
        _host = host;
    }

    public string Name => _host.Environment;

    public Version Version => Assembly.GetEntryAssembly()?.GetName().Version ?? new Version(0, 0);
    
    public string BaseAddress => _host.BaseAddress;
}