using Apollo.Components.Solutions;

namespace Apollo.Components.DynamicClient.Commands;

public record StartClient(SolutionModel Solution, string? EntryFile = null);
