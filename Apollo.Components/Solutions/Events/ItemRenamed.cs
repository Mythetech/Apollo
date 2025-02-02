namespace Apollo.Components.Solutions.Events;

public record ItemRenamed(ISolutionItem Item, string OldName, string NewName, bool ProjectRenamed);