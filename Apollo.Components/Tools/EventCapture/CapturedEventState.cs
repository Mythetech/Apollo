using System.Text.Json;

namespace Apollo.Components.Tools.EventCapture;

public sealed class CapturedEventState
{
    private readonly object _gate = new();
    private readonly List<CapturedEvent> _events = new();
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    public event Action? Changed;

    public int MaxEvents { get; set; } = 2000;

    public IReadOnlyList<CapturedEvent> GetEventsSnapshot()
    {
        lock (_gate)
        {
            return _events
                .OrderByDescending(e => e.Timestamp)
                .ToList();
        }
    }

    public IReadOnlyList<CapturedEventAggregate> GetAggregateSnapshot()
    {
        lock (_gate)
        {
            return _events
                .GroupBy(e => e.EventType)
                .Select(g => new CapturedEventAggregate(g.Key, g.Count()))
                .OrderByDescending(x => x.Count)
                .ToList();
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _events.Clear();
        }

        Changed?.Invoke();
    }

    public void Add(Type eventType, object payload)
    {
        var serialized = JsonSerializer.Serialize(payload, eventType, _serializerOptions);
        var captured = new CapturedEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            eventType.FullName ?? eventType.Name,
            serialized);

        lock (_gate)
        {
            _events.Add(captured);
            TrimToMax();
        }

        Changed?.Invoke();
    }

    private void TrimToMax()
    {
        if (MaxEvents <= 0)
            return;

        var overflow = _events.Count - MaxEvents;
        if (overflow <= 0)
            return;

        _events.RemoveRange(0, overflow);
    }
}
