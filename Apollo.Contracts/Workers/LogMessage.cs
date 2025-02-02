namespace Apollo.Contracts.Workers;

public record LogMessage(string Message, LogSeverity Severity, DateTimeOffset Timestamp);