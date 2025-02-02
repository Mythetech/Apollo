namespace Apollo.Components.Terminal;

public class CommandNotFoundException : Exception
{
    public string Command { get; }
    
    public CommandNotFoundException(string commandName)
    { 
        Command = commandName;
    }
} 