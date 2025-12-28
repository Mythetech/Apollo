namespace Apollo.Components.DynamicTabs.Commands;

public record UpdateTabLocation(Guid TabId, string Location);

public record UpdateTabLocationByName(string Name, string Location);