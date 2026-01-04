namespace Apollo.Components.Tools.EventCapture;

public sealed record CapturedEvent(
    Guid Id,
    DateTimeOffset Timestamp,
    string EventType,
    string Payload);

public sealed record CapturedEventAggregate(
    string EventType,
    int Count);
