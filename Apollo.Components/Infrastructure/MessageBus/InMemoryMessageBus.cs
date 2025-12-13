namespace Apollo.Components.Infrastructure.MessageBus;
using Microsoft.Extensions.Logging;
using Apollo.Components.Infrastructure.Environment;

public class InMemoryMessageBus : IMessageBus
{
    private readonly Dictionary<Type, List<Type>> _registeredConsumerTypes = new();
    private readonly Dictionary<Type, List<object>> _cachedConsumers = new();
    private readonly Dictionary<Type, List<object>> _subscribers = new();

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InMemoryMessageBus> _logger;
    private readonly CapturedEventState _capturedEventState;

    internal bool CaptureDebugInformation { get; }

    public InMemoryMessageBus(
        IServiceProvider serviceProvider,
        ILogger<InMemoryMessageBus> logger,
        CapturedEventState capturedEventState,
        IRuntimeEnvironment runtimeEnvironment)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _capturedEventState = capturedEventState;
        CaptureDebugInformation = runtimeEnvironment.IsDevelopment();
    }

    public async Task PublishAsync<TMessage>(TMessage message) where TMessage : class
    {
        if (CaptureDebugInformation)
        {
            try
            {
                _capturedEventState.Add(typeof(TMessage), message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing message {MessageType}", typeof(TMessage).Name);
            }
        }

        var registeredConsumers = GetOrResolveConsumers<TMessage>();

        var manualSubscribers = _subscribers.TryGetValue(typeof(TMessage), out var subscribers)
            ? subscribers.Cast<IConsumer<TMessage>>()
            : [];

        var allConsumers = registeredConsumers.Concat(manualSubscribers);

        var tasks = allConsumers.Select(async consumer =>
        {
            try
            {
                await consumer.Consume(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error in message bus consumer {ConsumerType} handling message {MessageType}", 
                    consumer.GetType().Name,
                    typeof(TMessage).Name);
            }
        });
        
        await Task.WhenAll(tasks);
    }

    public async Task PublishAsync(Type messageType, object message)
    {
        var method = typeof(InMemoryMessageBus)
            .GetMethod(nameof(PublishAsync), 1, [Type.MakeGenericMethodParameter(0)])!
            .MakeGenericMethod(messageType);
        
        await (Task)method.Invoke(this, [message])!;
    }

    public void RegisterConsumerType<TMessage, TConsumer>() where TMessage : class where TConsumer : IConsumer<TMessage>
    {
        if (!_registeredConsumerTypes.ContainsKey(typeof(TMessage)))
        {
            _registeredConsumerTypes[typeof(TMessage)] = new List<Type>();
        }

        _registeredConsumerTypes[typeof(TMessage)].Add(typeof(TConsumer));
    }

    private List<IConsumer<TMessage>> GetOrResolveConsumers<TMessage>() where TMessage : class
    {
        var messageType = typeof(TMessage);

        if (!_cachedConsumers.TryGetValue(messageType, out var cached))
        {
            if (!_registeredConsumerTypes.TryGetValue(messageType, out var consumerTypes))
                return [];

            cached = consumerTypes
                .Select(type => _serviceProvider.GetService(type))
                .Where(consumer => consumer is not null)
                .ToList();

            _cachedConsumers[messageType] = cached;
        }

        return cached.Cast<IConsumer<TMessage>>().ToList();
    }

    public void Subscribe<TMessage>(IConsumer<TMessage> consumer) where TMessage : class
    {
        if (!_subscribers.ContainsKey(typeof(TMessage)))
            _subscribers[typeof(TMessage)] = new List<object>();

        _subscribers[typeof(TMessage)].Add(consumer);
    }

    public void Unsubscribe<TMessage>(IConsumer<TMessage> consumer) where TMessage : class
    {
        if (_subscribers.TryGetValue(typeof(TMessage), out var handlers))
        {
            handlers.Remove(consumer);
            if (handlers.Count == 0)
                _subscribers.Remove(typeof(TMessage));
        }
    }
}