using Apollo.Components.Console;

namespace Apollo.Components.Testing
{
    public class TestConsoleOutputService
    {
      public event Action<ConsoleOutputViewModel>? OnConsoleOutputReceived;

        public event Action? OnConsoleCleared;

        private readonly List<ConsoleOutputViewModel> _logs = new();

        public IReadOnlyList<ConsoleOutputViewModel> Logs => _logs.AsReadOnly();
        
        public void AddLog(string message, ConsoleSeverity severity)
        {
            var logEntry = new ConsoleOutputViewModel
            {
                Timestamp = DateTimeOffset.Now,
                Severity = severity,
                Message = message
            };

            _logs.Add(logEntry);
            OnConsoleOutputReceived?.Invoke(logEntry); 
        }
        
        public void AddDebug(string message) => AddLog(message, ConsoleSeverity.Debug);
        
        public void AddInfo(string message) => AddLog(message, ConsoleSeverity.Info);
        
        public void AddWarning(string message) => AddLog(message, ConsoleSeverity.Warning);
        
        public void AddError(string message) => AddLog(message, ConsoleSeverity.Error);
        
        public void AddSuccess(string message) => AddLog(message, ConsoleSeverity.Success);

        public void ClearLogs()
        {
            _logs.Clear();
            OnConsoleCleared?.Invoke();
        }
    }
}