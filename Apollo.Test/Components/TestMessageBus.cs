using Apollo.Components.Infrastructure.MessageBus;

namespace Apollo.Test.Components;

public class TestMessageBus : IMessageBus
{
    public async Task PublishAsync<TMessage>(TMessage message) where TMessage : class
    {
        await Task.CompletedTask;
    }

    public async Task PublishAsync(Type messageType, object message)
    {
        await Task.CompletedTask;
    }

    public void RegisterConsumerType<TMessage, TConsumer>() where TMessage : class where TConsumer : IConsumer<TMessage>
    {
    }

    public void Subscribe<TMessage>(IConsumer<TMessage> consumer) where TMessage : class
    {
    }

    public void Unsubscribe<TMessage>(IConsumer<TMessage> consumer) where TMessage : class
    {
    }
}