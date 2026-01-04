using Mythetech.Framework.Infrastructure.MessageBus;

namespace Apollo.Test.Components;

public class TestMessageBus : IMessageBus
{
    public Task PublishAsync<TMessage>(TMessage message) where TMessage : class
    {
        return Task.CompletedTask;
    }

    public Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : class
    {
        return Task.CompletedTask;
    }

    public Task PublishAsync<TMessage>(TMessage message, PublishConfiguration configuration) where TMessage : class
    {
        return Task.CompletedTask;
    }

    public Task<TResponse> SendAsync<TMessage, TResponse>(TMessage message)
        where TMessage : class
        where TResponse : class
    {
        return Task.FromResult<TResponse>(default!);
    }

    public Task<TResponse> SendAsync<TMessage, TResponse>(TMessage message, QueryConfiguration configuration)
        where TMessage : class
        where TResponse : class
    {
        return Task.FromResult<TResponse>(default!);
    }

    public void RegisterConsumerType<TMessage, TConsumer>() where TMessage : class where TConsumer : IConsumer<TMessage>
    {
    }

    public void RegisterQueryHandler<TMessage, TResponse, THandler>()
        where TMessage : class
        where TResponse : class
        where THandler : IQueryHandler<TMessage, TResponse>
    {
    }

    public void Subscribe<TMessage>(IConsumer<TMessage> consumer) where TMessage : class
    {
    }

    public void Unsubscribe<TMessage>(IConsumer<TMessage> consumer) where TMessage : class
    {
    }
}