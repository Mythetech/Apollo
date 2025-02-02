namespace Apollo.Components.Infrastructure.MessageBus;

public interface IConsumer<TMessage>
{
    Task Consume(TMessage message);
}