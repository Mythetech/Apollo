using System.Reflection;
using Apollo.Components.Infrastructure.Environment;

namespace Apollo.Components.Terminal.CommandServices;

public class VersionCommand(TerminalState state, IRuntimeEnvironment environment) : ITerminalCommand
{
    public TerminalState State { get; init; } = state;
    public IRuntimeEnvironment Environment { get; } = environment;
    public string Name => "version";
    public string Description => "Displays Apollo version information";
    public string[] Aliases => new[] { "ver", "v" };

    public Task ExecuteAsync(string[] args)
    {
        var version = environment.Version.ToString();
        State?.AddEntry($"Apollo: {version}");
        State?.AddEntry("https://github.com/Mythetech/Apollo");
        return Task.CompletedTask;
    }
}