using Apollo.Contracts.Debugging;
using Apollo.Contracts.Solutions;

namespace Apollo.Debugging;

public record StartDebuggingMessage(Solution Solution, Breakpoint? Breakpoint);