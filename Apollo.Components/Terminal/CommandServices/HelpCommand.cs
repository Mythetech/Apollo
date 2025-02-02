using Apollo.Components.Terminal.Models;

namespace Apollo.Components.Terminal.CommandServices;

public class HelpCommand : ITerminalCommand
{
    public HelpCommand(TerminalState state) => State = state;
    public TerminalState State { get; init; }


    public string Name => "help";
    public string Description => "Lists available commands";
    public string[] Aliases => new[] { "?" };

    public Task ExecuteAsync(string[] args)
    {
        if (args.Length == 0)
        {
            foreach (var cmd in State.GetCommands())
            {
                State.AddEntry($"{cmd.Name} - {cmd.Description}");
            }
        }
        else
        {
            var commandName = args[0].ToLower();
            if (State.TryGetCommand(commandName, out var command))
            {
                State.AddEntry($"{command.Name} - {command.Description}");
                if (command.Aliases.Any())
                {
                    State.AddEntry($"Aliases: {string.Join(", ", command.Aliases)}");
                }
            }
            else
            {
                State.AddEntry($"Command not found: {commandName}", TerminalEntryType.Error);
            }
        }
        return Task.CompletedTask;
    }
}