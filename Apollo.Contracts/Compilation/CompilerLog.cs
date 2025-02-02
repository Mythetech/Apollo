using Apollo.Contracts.Workers;

namespace Apollo.Contracts.Compilation;

public record CompilerLog(string Message, LogSeverity Severity);