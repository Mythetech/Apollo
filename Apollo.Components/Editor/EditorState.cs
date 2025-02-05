namespace Apollo.Components.Editor;

public class EditorState
{
    public event Func<Task>? EditorStageChanged;
    public event Action<IReadOnlyDictionary<string, IEnumerable<int>>>? BreakpointsChanged;

    private Dictionary<string, HashSet<int>> _breakpoints = new();

    public IReadOnlyDictionary<string, IEnumerable<int>> Breakpoints => 
        _breakpoints.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.AsEnumerable());

    public void ToggleBreakpoint(string filePath, int lineNumber)
    {
        if (!_breakpoints.ContainsKey(filePath))
        {
            _breakpoints[filePath] = new HashSet<int>();
        }

        var fileBreakpoints = _breakpoints[filePath];
        if (!fileBreakpoints.Add(lineNumber))
        {
            fileBreakpoints.Remove(lineNumber);
        }

        BreakpointsChanged?.Invoke(Breakpoints);
    }
}