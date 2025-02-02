namespace Apollo.Components.Terminal.CommandServices;

public class ClearCommand : ITerminalCommand
{
    public ClearCommand(TerminalState state) => State = state;
    public TerminalState State { get; init; }
    public string Name => "clear";
    public string Description => "Clears the terminal";
    public string[] Aliases => new[] { "cls" };

    public Task ExecuteAsync(string[] args)
    {
        State?.Terminal?.Clear();
        return Task.CompletedTask;
    }
}