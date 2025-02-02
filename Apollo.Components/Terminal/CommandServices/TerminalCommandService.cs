namespace Apollo.Components.Terminal.CommandServices;

public abstract class TerminalCommandService
{
    private readonly Dictionary<string, ITerminalCommand> _commands = new();

    public event Action? TerminalStateChanged;
    protected void NotifyTerminalStateChanged() => TerminalStateChanged?.Invoke();

    public List<string> Output { get; set; } = [];
    
    protected TerminalCommandService()
    {
    }

    public void Register(ITerminalCommand command)
    {
        _commands[command.Name.ToLower()] = command;
        foreach (var alias in command.Aliases)
        {
            _commands[alias.ToLower()] = command;
        }
    }

    public async Task ExecuteAsync(string commandLine)
    {
        var args = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length == 0) return;

        var commandName = args[0].ToLower();
        if (_commands.TryGetValue(commandName, out var command))
        {
            await command.ExecuteAsync(args.Skip(1)?.ToArray() ?? []);
        }
        else
        {
            throw new CommandNotFoundException(commandName);
        }
    }

    public IEnumerable<ITerminalCommand> GetCommands() => _commands.Values.Distinct();
}