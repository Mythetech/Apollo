using Mythetech.Framework.Infrastructure.MessageBus;

namespace Apollo.Components.Tools.EventCapture;

/// <summary>
/// A message pipe that captures all events for debugging/viewing in the Event Viewer.
/// </summary>
public sealed class CapturedEventPipe : IMessagePipe
{
    private readonly CapturedEventState _state;

    public CapturedEventPipe(CapturedEventState state)
    {
        _state = state;
    }

    public Task<bool> ProcessAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
        where TMessage : class
    {
        _state.Add(typeof(TMessage), message);
        return Task.FromResult(true);
    }
}
