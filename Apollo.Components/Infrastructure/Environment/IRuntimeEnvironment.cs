namespace Apollo.Components.Infrastructure.Environment;

public interface IRuntimeEnvironment
{
    public string Name { get; }
    
    public Version Version { get; }
    
    public string BaseAddress { get; }
}

public static class IRuntimeEnvironmentExtensions
{
    public static bool IsEnvironment(this IRuntimeEnvironment runtime, string environment)
        => string.Equals(environment, runtime.Name, StringComparison.OrdinalIgnoreCase);
    
    public static bool IsDevelopment(this IRuntimeEnvironment runtime)
        => runtime.IsEnvironment("development");
    
    public static bool IsProduction(this IRuntimeEnvironment runtime)
     => runtime.IsEnvironment("production");
}