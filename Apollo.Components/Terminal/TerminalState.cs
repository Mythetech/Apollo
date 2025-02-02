using System.Windows.Input;
using Apollo.Components.Terminal.CommandServices;
using Apollo.Components.Terminal.Models;

namespace Apollo.Components.Terminal;

public class TerminalState : TerminalCommandService
{
    public ApolloTerminal? Terminal { get; set; }
    
    private const int MaxHistorySize = 50;
    private readonly List<string> _commandHistory = new();
    private int _historyIndex = -1;
    private const int MaxEntries = 1000;

    public List<TerminalEntry> Entries { get; } = new();

    public void AddEntry(string content, TerminalEntryType type = TerminalEntryType.Standard, string? customClass = null, bool isCommand = false)
    {
        Entries.Add(new TerminalEntry
        {
            Content = content,
            Type = type,
            CustomClass = customClass,
            IsCommand = isCommand
        });

        // Trim old entries if we exceed the maximum
        if (Entries.Count > MaxEntries)
        {
            Entries.RemoveRange(0, Entries.Count - MaxEntries);
        }

        NotifyTerminalStateChanged();
    }

    public void AddToHistory(string command)
    {
        _commandHistory.Insert(0, command);
        if (_commandHistory.Count > MaxHistorySize)
        {
            _commandHistory.RemoveAt(_commandHistory.Count - 1);
        }
        _historyIndex = -1;
    }

    public string? GetPreviousCommand()
    {
        if (_commandHistory.Count == 0) return null;
        
        _historyIndex = Math.Min(_historyIndex + 1, _commandHistory.Count - 1);
        return _commandHistory[_historyIndex];
    }

    public string? GetNextCommand()
    {
        if (_historyIndex < 0) return null;
        
        _historyIndex--;
        return _historyIndex >= 0 ? _commandHistory[_historyIndex] : string.Empty;
    }

    public void Clear()
    {
        Entries.Clear();
        NotifyTerminalStateChanged();
    }

    public bool TryGetCommand(string commandName, out ITerminalCommand? command)
    {
        command = GetCommands()?.FirstOrDefault(x => x.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
        return command != null;
    }
}