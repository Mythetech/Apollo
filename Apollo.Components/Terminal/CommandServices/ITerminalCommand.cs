namespace Apollo.Components.Terminal.CommandServices;

public interface ITerminalCommand
{
    string Name { get; }
    string Description { get; }
    string[] Aliases { get; }
    Task ExecuteAsync(string[] args);
}